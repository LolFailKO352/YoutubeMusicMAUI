using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Melodium.Services;

/// <summary>
/// Převede fragmentovaný MP4 (DASH/fMP4, jak ho servíruje YouTube – itag 140)
/// na surový AAC stream s ADTS hlavičkami (.aac).
///
/// Proč: Windows Media Foundation (a tím pádem NAudio MediaFoundationReader)
/// neumí přehrát fragmentovaný MP4 ze souboru – přečte prázdnou tabulku samplů
/// v <c>moov</c> a okamžitě skončí. ADTS AAC ale MF přehraje bez problému.
///
/// Algoritmus (ověřený na reálných souborech – počet samplů přesně odpovídá délce):
///  1) z <c>moov/trak/.../stsd/mp4a/esds</c> se vyčte AudioSpecificConfig
///     (profil, index vzorkovací frekvence, počet kanálů),
///  2) projdou se všechny <c>moof</c> fragmenty; z <c>tfhd</c>/<c>trun</c> se zjistí
///     velikosti samplů a offset dat (vůči začátku <c>moof</c>),
///  3) každý sample (jeden AAC frame) se zabalí do 7bajtové ADTS hlavičky.
/// </summary>
public static class FragmentedMp4Demuxer
{
    /// <summary>Vrací true, pokud soubor obsahuje fragmenty (moof) = je potřeba demux.</summary>
    public static bool IsFragmented(byte[] data)
    {
        foreach (var (type, _, _, _) in Boxes(data, 0, data.Length))
        {
            if (type == "moof") return true;
            if (type == "mdat") return false; // progresivní soubor: mdat dřív než jakýkoli moof
        }
        return false;
    }

    /// <summary>
    /// Demuxuje fragmentovaný MP4 do ADTS .aac. Vrací cestu k novému souboru.
    /// Pokud soubor není fragmentovaný, vrací <paramref name="sourcePath"/> beze změny.
    /// </summary>
    public static string DemuxToAacFile(string sourcePath)
    {
        var data = File.ReadAllBytes(sourcePath);
        if (!IsFragmented(data))
            return sourcePath;

        var (profile, freqIndex, channels) = ReadAudioConfig(data)
            ?? throw new InvalidDataException("Nepodařilo se přečíst AudioSpecificConfig (esds).");

        var outPath = Path.ChangeExtension(sourcePath, ".aac");
        using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);

        var adts = new byte[7];
        foreach (var (type, off, hdr, size) in Boxes(data, 0, data.Length))
        {
            if (type != "moof") continue;
            int moofStart = off;
            int moofEnd = off + size;

            var traf = Find(data, off + hdr, moofEnd, "traf");
            if (traf is null) continue;
            var (trafStart, trafEnd) = traf.Value;

            uint defaultSampleSize = 0;
            long baseOffset = moofStart; // default-base-is-moof (standard u CMAF/DASH)

            // tfhd – výchozí hodnoty
            var tfhd = Find(data, trafStart, trafEnd, "tfhd");
            if (tfhd is { } t)
            {
                int p = t.start;
                uint flags = ReadU32(data, p) & 0xFFFFFF;
                p += 4 + 4; // version/flags + track_ID
                if ((flags & 0x000001) != 0) { baseOffset = (long)ReadU64(data, p); p += 8; }
                if ((flags & 0x000002) != 0) p += 4;
                if ((flags & 0x000008) != 0) p += 4;
                if ((flags & 0x000010) != 0) { defaultSampleSize = ReadU32(data, p); p += 4; }
            }

            // trun – velikosti samplů + offset dat
            var trun = Find(data, trafStart, trafEnd, "trun");
            if (trun is null) continue;
            int q = trun.Value.start;
            uint trunFlags = ReadU32(data, q) & 0xFFFFFF;
            q += 4;
            uint sampleCount = ReadU32(data, q); q += 4;
            int dataOffset = 0;
            if ((trunFlags & 0x000001) != 0) { dataOffset = ReadI32(data, q); q += 4; }
            if ((trunFlags & 0x000004) != 0) q += 4; // first-sample-flags

            long cur = baseOffset + dataOffset;
            for (uint i = 0; i < sampleCount; i++)
            {
                if ((trunFlags & 0x000100) != 0) q += 4;            // sample-duration
                uint sampleSize = defaultSampleSize;
                if ((trunFlags & 0x000200) != 0) { sampleSize = ReadU32(data, q); q += 4; }
                if ((trunFlags & 0x000400) != 0) q += 4;            // sample-flags
                if ((trunFlags & 0x000800) != 0) q += 4;            // composition-time-offset

                int frameLen = (int)sampleSize + 7;
                WriteAdtsHeader(adts, frameLen, profile, freqIndex, channels);
                fs.Write(adts, 0, 7);
                fs.Write(data, (int)cur, (int)sampleSize);
                cur += sampleSize;
            }
        }

        return outPath;
    }

    private static void WriteAdtsHeader(byte[] h, int frameLen, int profile, int freqIndex, int channels)
    {
        h[0] = 0xFF;
        h[1] = 0xF1; // MPEG-4, Layer 0, protection absent
        h[2] = (byte)(((profile & 3) << 6) | ((freqIndex & 0xF) << 2) | ((channels >> 2) & 1));
        h[3] = (byte)(((channels & 3) << 6) | ((frameLen >> 11) & 3));
        h[4] = (byte)((frameLen >> 3) & 0xFF);
        h[5] = (byte)(((frameLen & 7) << 5) | 0x1F);
        h[6] = 0xFC;
    }

    // Vrací (profile, freqIndex, channelConfig) z esds → AudioSpecificConfig.
    private static (int profile, int freqIndex, int channels)? ReadAudioConfig(byte[] data)
    {
        var stsd = FindPath(data, 0, data.Length, "moov", "trak", "mdia", "minf", "stbl", "stsd");
        if (stsd is null) return null;

        // stsd: 4 (version/flags) + 4 (entry_count) -> vnořené entry (mp4a)
        var mp4a = Find(data, stsd.Value.start + 8, stsd.Value.end, "mp4a");
        if (mp4a is null) return null;

        // mp4a sample entry: 28 bajtů hlavičky před vnořeným esds
        var esds = Find(data, mp4a.Value.start + 28, mp4a.Value.end, "esds");
        if (esds is null) return null;

        int p = esds.Value.start + 4; // přeskoč version/flags
        if (data[p++] != 0x03) return null;       // ES_Descriptor
        ReadDescLen(data, ref p);
        p += 3;                                     // ES_ID(2) + flags(1)
        if (data[p++] != 0x04) return null;        // DecoderConfigDescriptor
        ReadDescLen(data, ref p);
        p += 1 + 1 + 3 + 4 + 4;                     // objType + streamType + bufferSize(3) + max(4) + avg(4)
        if (data[p++] != 0x05) return null;        // DecoderSpecificInfo
        int ascLen = ReadDescLen(data, ref p);
        if (ascLen < 2) return null;

        // AudioSpecificConfig: 5 bit AOT, 4 bit freqIndex, 4 bit channelConfig
        int b0 = data[p], b1 = data[p + 1];
        int aot = b0 >> 3;
        int freqIndex = ((b0 & 0x07) << 1) | (b1 >> 7);
        int channels = (b1 >> 3) & 0x0F;
        return (aot - 1, freqIndex, channels);
    }

    private static int ReadDescLen(byte[] data, ref int p)
    {
        int len = 0;
        while (true)
        {
            int c = data[p++];
            len = (len << 7) | (c & 0x7F);
            if ((c & 0x80) == 0) break;
        }
        return len;
    }

    // ---- pomocné: procházení MP4 boxů ----

    private static IEnumerable<(string type, int off, int hdr, int size)> Boxes(byte[] data, int start, int end)
    {
        int off = start;
        while (off + 8 <= end)
        {
            long size = ReadU32(data, off);
            int hdr = 8;
            if (size == 1) { size = (long)ReadU64(data, off + 8); hdr = 16; }
            else if (size == 0) { size = end - off; }
            if (size < hdr || off + size > end) yield break;

            string type = System.Text.Encoding.ASCII.GetString(data, off + 4, 4);
            yield return (type, off, hdr, (int)size);
            off += (int)size;
        }
    }

    private static (int start, int end)? Find(byte[] data, int start, int end, string type)
    {
        foreach (var (t, off, hdr, size) in Boxes(data, start, end))
            if (t == type) return (off + hdr, off + size);
        return null;
    }

    private static (int start, int end)? FindPath(byte[] data, int start, int end, params string[] path)
    {
        int s = start, e = end;
        foreach (var p in path)
        {
            var r = Find(data, s, e, p);
            if (r is null) return null;
            (s, e) = r.Value;
        }
        return (s, e);
    }

    private static uint ReadU32(byte[] d, int o) => BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(o, 4));
    private static int ReadI32(byte[] d, int o) => BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(o, 4));
    private static ulong ReadU64(byte[] d, int o) => BinaryPrimitives.ReadUInt64BigEndian(d.AsSpan(o, 8));
}
