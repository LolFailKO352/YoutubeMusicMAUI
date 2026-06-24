import re

with open('MainPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Replace ItemTemplate
old_item_template = """<CollectionView.ItemTemplate>
                                            <DataTemplate x:DataType="models:SongModel">
                                                <Border Stroke="{AppThemeBinding Light={StaticResource WinUIBorderLight}, Dark=Transparent}" StrokeThickness="1" StrokeShape="RoundRectangle 4" BackgroundColor="{AppThemeBinding Light={StaticResource WinUICardBgLight}, Dark=#202020}" Margin="0,0,0,6" Padding="10,8">
                                                    <Grid ColumnDefinitions="44, *, Auto" ColumnSpacing="12">
                                                        <Border Grid.Column="0" Stroke="Transparent" StrokeShape="RoundRectangle 4">
                                                            <Image Source="{Binding ThumbnailUrl}" Aspect="AspectFill" HeightRequest="38" WidthRequest="38" />
                                                        </Border>
                                                        <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                                            <Label Text="{Binding Title}" FontAttributes="Bold" FontSize="13" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" LineBreakMode="TailTruncation" />
                                                            <Label Text="{Binding Artist}" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="11" LineBreakMode="TailTruncation" />
                                                        </VerticalStackLayout>
                                                        <Label Grid.Column="2" VerticalOptions="Center" Margin="0,0,10,0" FontSize="12" TextColor="{AppThemeBinding Light={StaticResource WinUIAccentLight}, Dark={StaticResource WinUIAccentDark}}">
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
                                        </CollectionView.ItemTemplate>"""

new_item_template = """<CollectionView.ItemTemplate>
                                            <DataTemplate x:DataType="models:SongModel">
                                                <Border Stroke="Transparent" StrokeShape="RoundRectangle 8" BackgroundColor="Transparent" Padding="12,12">
                                                    <VisualStateManager.VisualStateGroups>
                                                        <VisualStateGroup x:Name="CommonStates">
                                                            <VisualState x:Name="Normal" />
                                                            <VisualState x:Name="Selected">
                                                                <VisualState.Setters>
                                                                    <Setter Property="BackgroundColor" Value="{AppThemeBinding Light=#EAEAEA, Dark={StaticResource WinUISelectedDark}}" />
                                                                </VisualState.Setters>
                                                            </VisualState>
                                                        </VisualStateGroup>
                                                    </VisualStateManager.VisualStateGroups>
                                                    <Grid ColumnDefinitions="40, 3*, 2*, 2*, 2*, 60" ColumnSpacing="16" VerticalOptions="Center">
                                                        <Label Grid.Column="0" Text="&#xE768;" FontFamily="Segoe Fluent Icons" FontSize="12" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUIAccentDark}}" VerticalOptions="Center" HorizontalOptions="Center" />
                                                        
                                                        <Label Grid.Column="1" Text="{Binding Title}" FontAttributes="Bold" FontSize="13" TextColor="{AppThemeBinding Light={StaticResource WinUITextPrimaryLight}, Dark={StaticResource WinUITextPrimaryDark}}" LineBreakMode="TailTruncation" VerticalOptions="Center" />
                                                        
                                                        <Label Grid.Column="2" Text="{Binding Artist}" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUIAccentDark}}" FontSize="12" LineBreakMode="TailTruncation" VerticalOptions="Center" />
                                                        
                                                        <Label Grid.Column="3" Text="{Binding Title}" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="12" LineBreakMode="TailTruncation" VerticalOptions="Center" />
                                                        
                                                        <Label Grid.Column="4" Text="Unknown genre" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="12" LineBreakMode="TailTruncation" VerticalOptions="Center" />
                                                        
                                                        <Label Grid.Column="5" Text="3:31" TextColor="{AppThemeBinding Light={StaticResource WinUITextSecondaryLight}, Dark={StaticResource WinUITextSecondaryDark}}" FontSize="12" HorizontalOptions="End" VerticalOptions="Center" />
                                                    </Grid>
                                                </Border>
                                            </DataTemplate>
                                        </CollectionView.ItemTemplate>"""

xaml = xaml.replace(old_item_template, new_item_template)

with open('new_player.xml', 'r', encoding='utf-8') as f:
    new_player = f.read()

# Replace Bottom Player
start_idx = xaml.find("<!-- SPODNÍ PANEL PŘEHRÁVAČE")
end_idx = xaml.rfind("</Grid>\n    </Grid>\n\n</ContentPage>")

if start_idx != -1 and end_idx != -1:
    xaml = xaml[:start_idx] + new_player + "\n    " + xaml[end_idx:]
else:
    print(f"Error: start={start_idx}, end={end_idx}")

with open('MainPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

print("Patched MainPage.xaml")
