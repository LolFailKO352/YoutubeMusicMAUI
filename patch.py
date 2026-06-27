import sys

with open('MainPage.xaml', 'r', encoding='utf-8') as f:
    content = f.read()

start_idx = content.find('<!-- FULLSCREEN PLAYER OVERLAY -->')
if start_idx == -1:
    print('Could not find start block')
    sys.exit(1)

# Find </ContentPage>
end_idx = content.find('</ContentPage>', start_idx)
if end_idx == -1:
    print('Could not find end block')
    sys.exit(1)

new_fullscreen = '''<!-- FULLSCREEN PLAYER OVERLAY -->
        <Grid x:Name="FullScreenGrid" 
              Grid.RowSpan="3" 
              Grid.ColumnSpan="2" 
              IsVisible="False"
              BackgroundColor="{AppThemeBinding Light={StaticResource WinUIBgLight}, Dark={StaticResource WinUIBgDark}}"
              ZIndex="100">
            <!-- Blur Background -->
            <Image Source="{Binding CurrentSong.ThumbnailUrl}" Aspect="AspectFill" Opacity="0.4" />
            <BoxView Color="{AppThemeBinding Light=#E6F3F3F3, Dark=#E6141414}" />

            <Grid ColumnDefinitions="6*, 4*" Margin="0, 0, 32, 0">
                
                <!-- LEVÁ ČÁST: PŘEHRÁVAČ -->
                <Grid Grid.Column="0">
                    <!-- Close Button -->
                    <Button Command="{Binding ToggleFullScreenPlayerCommand}" 
                            Text="&#xE711;" FontFamily="Segoe Fluent Icons" FontSize="18"
                            BackgroundColor="Transparent" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}"
                            HorizontalOptions="Start" VerticalOptions="Start" Margin="32" WidthRequest="44" HeightRequest="44" />

                    <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center" Spacing="24" WidthRequest="500" MaximumWidthRequest="800">
                        <Border Stroke="Transparent" StrokeShape="RoundRectangle 24" WidthRequest="400" HeightRequest="400" HorizontalOptions="Center">
                            <Image Source="{Binding CurrentSong.ThumbnailUrl}" Aspect="AspectFill" />
                        </Border>
                        <Label Text="{Binding CurrentSong.Title}" FontSize="32" FontAttributes="Bold" HorizontalOptions="Center" HorizontalTextAlignment="Center" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" />
                        <Label Text="{Binding CurrentSong.Artist}" FontSize="18" HorizontalOptions="Center" HorizontalTextAlignment="Center" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" />

                        <Grid ColumnDefinitions="Auto, *, Auto" Margin="0,32,0,0">
                            <Label Grid.Column="0" Text="{Binding PositionText}" VerticalOptions="Center" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" />
                            <Slider Grid.Column="1" Minimum="0" Maximum="{Binding DurationSeconds}" Value="{Binding PositionSeconds}" DragStarted="OnSliderDragStarted" DragCompleted="OnSliderDragCompleted" MinimumTrackColor="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" MaximumTrackColor="{AppThemeBinding Light=#D2D2D2, Dark=#3C3C3C}" ThumbColor="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" Margin="16,0" />
                            <Label Grid.Column="2" Text="{Binding DurationText}" VerticalOptions="Center" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" />
                        </Grid>

                        <HorizontalStackLayout HorizontalOptions="Center" Spacing="32" Margin="0,16,0,0">
                            <Button Command="{Binding ToggleShuffleCommand}" Text="&#xE8B1;" FontFamily="Segoe Fluent Icons" BackgroundColor="Transparent" FontSize="20" WidthRequest="48" HeightRequest="48" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}">
                                <Button.Triggers>
                                    <DataTrigger TargetType="Button" Binding="{Binding IsShuffle}" Value="True">
                                        <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" />
                                    </DataTrigger>
                                </Button.Triggers>
                            </Button>
                            <Button Command="{Binding PlayPreviousSongCommand}" Text="&#xE892;" FontFamily="Segoe Fluent Icons" BackgroundColor="Transparent" FontSize="24" WidthRequest="64" HeightRequest="64" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" />
                            <Button Command="{Binding PlayPauseCommand}" BackgroundColor="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" TextColor="White" CornerRadius="32" FontSize="24" WidthRequest="64" HeightRequest="64">
                                <Button.FontFamily><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="Segoe Fluent Icons" /></OnPlatform></Button.FontFamily>
                                <Button.Text><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="&#xE768;" /><On Platform="Default" Value="▶" /></OnPlatform></Button.Text>
                                <Button.Triggers>
                                    <DataTrigger TargetType="Button" Binding="{Binding IsPlaying}" Value="True">
                                        <Setter Property="Text"><Setter.Value><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="&#xE769;" /><On Platform="Default" Value="⏸" /></OnPlatform></Setter.Value></Setter>
                                    </DataTrigger>
                                </Button.Triggers>
                            </Button>
                            <Button Command="{Binding PlayNextSongCommand}" Text="&#xE893;" FontFamily="Segoe Fluent Icons" BackgroundColor="Transparent" FontSize="24" WidthRequest="64" HeightRequest="64" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" />
                            <Button Command="{Binding ToggleRepeatCommand}" BackgroundColor="Transparent" FontSize="20" WidthRequest="48" HeightRequest="48">
                                <Button.FontFamily><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="Segoe Fluent Icons" /></OnPlatform></Button.FontFamily>
                                <Button.Triggers>
                                    <DataTrigger TargetType="Button" Binding="{Binding RepeatMode}" Value="0">
                                        <Setter Property="Text"><Setter.Value><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="&#xE8EE;" /><On Platform="Default" Value="🔁" /></OnPlatform></Setter.Value></Setter>
                                        <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" />
                                    </DataTrigger>
                                    <DataTrigger TargetType="Button" Binding="{Binding RepeatMode}" Value="1">
                                        <Setter Property="Text"><Setter.Value><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="&#xE8EE;" /><On Platform="Default" Value="🔁" /></OnPlatform></Setter.Value></Setter>
                                        <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" />
                                    </DataTrigger>
                                    <DataTrigger TargetType="Button" Binding="{Binding RepeatMode}" Value="2">
                                        <Setter Property="Text"><Setter.Value><OnPlatform x:TypeArguments="x:String"><On Platform="WinUI" Value="&#xE8ED;" /><On Platform="Default" Value="🔂" /></OnPlatform></Setter.Value></Setter>
                                        <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" />
                                    </DataTrigger>
                                </Button.Triggers>
                            </Button>
                        </HorizontalStackLayout>
                    </VerticalStackLayout>
                </Grid>

                <!-- PRAVÁ ČÁST: FRONTA -->
                <Grid Grid.Column="1" RowDefinitions="Auto, *" Margin="0,64,0,32">
                    <Grid Grid.Row="0" ColumnDefinitions="*, Auto" Margin="0,0,0,16">
                        <VerticalStackLayout Grid.Column="0" Spacing="4">
                            <Label Text="{Binding TextPlaybackQueue}" FontSize="24" FontAttributes="Bold" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" />
                            <Label TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="13">
                                <Label.Text>
                                    <MultiBinding StringFormat="{}{0} {1}">
                                        <Binding Path="TextSongsInQueueLabel" />
                                        <Binding Path="PlaybackQueue.Count" />
                                    </MultiBinding>
                                </Label.Text>
                            </Label>                        
                        </VerticalStackLayout>
                        <Button Grid.Column="1" 
                                Text="{Binding TextClearQueue}" 
                                Command="{Binding ClearQueueCommand}" 
                                Style="{StaticResource WinUISecondaryBtn}" 
                                VerticalOptions="Center" />
                    </Grid>

                    <!-- Queue List -->
                    <CollectionView Grid.Row="1" 
                                    ItemsSource="{Binding PlaybackQueue}"
                                    SelectionMode="Single"
                                    SelectedItem="{Binding SelectedQueueSong, Mode=TwoWay}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="models:SongModel">
                                <Border Stroke="{AppThemeBinding Light={StaticResource WinUIBorderLight}, Dark=Transparent}" StrokeThickness="1" StrokeShape="RoundRectangle 4" BackgroundColor="{AppThemeBinding Light={StaticResource WinUICardBgLight}, Dark=#202020}" Margin="0,0,16,6" Padding="10,8">
                                    <VisualStateManager.VisualStateGroups>
                                        <VisualStateGroup x:Name="CommonStates">
                                            <VisualState x:Name="Normal" />
                                            <VisualState x:Name="Selected">
                                                <VisualState.Setters>
                                                    <Setter Property="BackgroundColor" Value="{AppThemeBinding Light=#D2E8FF, Dark=#2A3D50}" />
                                                    <Setter Property="Stroke" Value="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}" />
                                                    <Setter Property="StrokeThickness" Value="1" />
                                                </VisualState.Setters>
                                            </VisualState>
                                            <VisualState x:Name="PointerOver">
                                                <VisualState.Setters>
                                                    <Setter Property="BackgroundColor" Value="{AppThemeBinding Light=#F3F3F3, Dark=#2D2D2D}" />
                                                </VisualState.Setters>
                                            </VisualState>
                                        </VisualStateGroup>
                                    </VisualStateManager.VisualStateGroups>
                                    
                                    <Grid ColumnDefinitions="48, *, Auto" ColumnSpacing="15">
                                        <Border Grid.Column="0" Stroke="Transparent" StrokeShape="RoundRectangle 4" HeightRequest="44" WidthRequest="44">
                                            <Image Source="{Binding ThumbnailUrl}" Aspect="AspectFill" />
                                        </Border>

                                        <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                            <Label Text="{Binding Title}" FontAttributes="Bold" FontSize="13" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" LineBreakMode="TailTruncation" />
                                            <Label Text="{Binding Artist}" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="11" LineBreakMode="TailTruncation" />
                                        </VerticalStackLayout>

                                        <Label Grid.Column="2" VerticalOptions="Center" Margin="0,0,10,0" FontSize="13" TextColor="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}">
                                            <Label.FontFamily>
                                                <OnPlatform x:TypeArguments="x:String">
                                                    <On Platform="WinUI" Value="Segoe Fluent Icons" />
                                                </OnPlatform>
                                            </Label.FontFamily>
                                            <Label.Text>
                                                <OnPlatform x:TypeArguments="x:String">
                                                    <On Platform="WinUI" Value="&#xE768;" />
                                                    <On Platform="Default" Value="▶" />
                                                </OnPlatform>
                                            </Label.Text>
                                        </Label>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>

                    <!-- Empty state -->
                    <VerticalStackLayout Grid.Row="1" 
                                         VerticalOptions="Center" 
                                         HorizontalOptions="Center" 
                                         Spacing="12" 
                                         IsVisible="{Binding IsCurrentSongNull}">
                        <Label Text="{Binding TextEmptyQueue}" FontSize="18" FontAttributes="Bold" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" HorizontalOptions="Center" />
                        <Label Text="{Binding TextEmptyQueueDesc}" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="13" HorizontalOptions="Center" />
                    </VerticalStackLayout>
                </Grid>
            </Grid>
        </Grid>'''

new_content = content[:start_idx] + new_fullscreen + '\n    </Grid>\n\n</ContentPage>'
with open('MainPage.xaml', 'w', encoding='utf-8') as f:
    f.write(new_content)
print('Successfully replaced FullScreenGrid!')
