using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using NAudio.Wave;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Threading;
using TagLib;
using System.Linq;
using System.Windows.Media.Imaging;

namespace WpfApp9
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool isPlaying = false;
        private AudioFileReader reader;
        private WaveOut waveOut;
        private List<string> musicFiles;
        private int currentMusicIndex = 0;
        private string selectedFolderPath;
        private DispatcherTimer uiTimer;

        private TimeSpan currentTime;
        public TimeSpan CurrentTime
        {
            get { return currentTime; }
            set
            {
                currentTime = value;
                OnPropertyChanged(nameof(CurrentTime));
            }
        }

        private TimeSpan totalTime;
        public TimeSpan TotalTime
        {
            get { return totalTime; }
            set
            {
                totalTime = value;
                OnPropertyChanged(nameof(TotalTime));
            }
        }

        private double sliderValue;
        public double SliderValue
        {
            get { return sliderValue; }
            set
            {
                sliderValue = value;
                OnPropertyChanged(nameof(SliderValue));
            }
        }

        private float volume;
        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string songTitle;
        public string SongTitle
        {
            get { return songTitle; }
            set
            {
                songTitle = value;
                OnPropertyChanged(nameof(SongTitle));
            }
        }

        private string author;
        public string Author
        {
            get { return author; }
            set
            {
                author = value;
                OnPropertyChanged(nameof(Author));
            }
        }

        private string album;
        public string Album
        {
            get { return album; }
            set
            {
                album = value;
                OnPropertyChanged(nameof(Album));
            }
        }

        private string genre;
        public string Genre
        {
            get { return genre; }
            set
            {
                genre = value;
                OnPropertyChanged(nameof(Genre));
            }
        }

        private string releaseYear;
        public string ReleaseYear
        {
            get { return releaseYear; }
            set
            {
                releaseYear = value;
                OnPropertyChanged(nameof(ReleaseYear));
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(15000);
            uiTimer.Tick += UITimer_Tick;
            uiTimer.Start();
        }

        private void UITimer_Tick(object sender, EventArgs e)
        {
            UpdateTimeInfo();
        }

        private void UpdateTimeInfo()
        {
            if (reader != null && waveOut != null)
            {
                CurrentTime = reader.CurrentTime;
                TotalTime = reader.TotalTime;
                SliderValue = reader.Position / (double)reader.Length;
            }
        }


        private string GetAuthorNameFromMetadata(string filePath)
        {
            try
            {
                if (Path.GetExtension(filePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    using (var mp3File = TagLib.File.Create(filePath))
                    {
                        if (mp3File != null && mp3File.Tag != null)
                        {
                            var artists = mp3File.Tag.Performers;
                            if (artists != null && artists.Length > 0)
                            {
                                return artists[0];
                            }
                        }
                    }
                }
                else if (Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    using (var wavFile = TagLib.File.Create(filePath))
                    {
                        if (wavFile != null && wavFile.Tag != null)
                        {
                            var artists = wavFile.Tag.Performers;
                            if (artists != null && artists.Length > 0)
                            {
                                return artists[0];
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving metadata: {ex.Message}");
            }

            return "Unknown Author"; 
        }

        private string GetAlbumFromMetadata(string filePath)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                if (file != null && file.Tag != null)
                {
                    return file.Tag.Album ?? "Unknown Album";
                }
            }
            return "Unknown Album";
        }

        private string GetGenreFromMetadata(string filePath)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                if (file != null && file.Tag != null)
                {
                    return file.Tag.FirstGenre ?? "Unknown Genre";
                }
            }
            return "Unknown Genre";
        }

        private string GetReleaseYearFromMetadata(string filePath)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                if (file != null && file.Tag != null)
                {
                    return file.Tag.Year > 0 ? file.Tag.Year.ToString() : "Unknown Year";
                }
            }
            return "Unknown Year";
        }

        private string lyrics;
        public string Lyrics
        {
            get { return lyrics; }
            set
            {
                lyrics = value;
                OnPropertyChanged(nameof(Lyrics));
            }
        }

        private void LoadAndDisplayLyrics(string filePath)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                if (file.Tag.Comment != null && !string.IsNullOrEmpty(file.Tag.Comment))
                {
                    Lyrics = file.Tag.Comment;
                }
                else
                {
                    Lyrics = "Lyrics not available.";
                }
            }
        }


        private void PlayMusicAtIndex(int index)
        {
            if (musicFiles != null && index >= 0 && index < musicFiles.Count)
            {
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }

                reader = new AudioFileReader(musicFiles[index]);
                waveOut = new WaveOut();
                waveOut.Init(reader);
                waveOut.Play();
                isPlaying = true;

                UpdateTimeInfo();

                SongTitle = Path.GetFileNameWithoutExtension(musicFiles[index]);
                Author = GetAuthorNameFromMetadata(musicFiles[index]);
                Album = GetAlbumFromMetadata(musicFiles[index]);
                Genre = GetGenreFromMetadata(musicFiles[index]);
                ReleaseYear = GetReleaseYearFromMetadata(musicFiles[index]);

                LoadAndDisplayCoverArt(musicFiles[index]);

                LoadAndDisplayLyrics(musicFiles[index]);
            }
        }


        private void LoadAndDisplayCoverArt(string filePath)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                if (file != null && file.Tag.Pictures.Length > 0)
                {
                    var picture = file.Tag.Pictures[0];
                    using (var stream = new MemoryStream(picture.Data.Data))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();

                        CoverArtImage.Source = image;
                    }
                }
            }
        }

        private void PlayMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                SelectMusicFolder();
            }

            if (!isPlaying)
            {
                if (waveOut != null)
                {
                    waveOut.Play();
                    isPlaying = true;
                }
                else
                {
                    PlayMusicAtIndex(currentMusicIndex);
                }
            }
        }


        private void PauseMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                isPlaying = false;
            }
        }


        private void StopMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Stop();

                waveOut.Dispose();
                waveOut = null;

            }

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            isPlaying = false;
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SelectMusicFolder();
        }

        private void SelectMusicFolder()
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select a Music Folder",
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                selectedFolderPath = folderDialog.FileName;
                musicFiles = new List<string>();

                string[] supportedExtensions = { ".mp3", ".wav", ".ogg" };
                foreach (var extension in supportedExtensions)
                {
                    musicFiles.AddRange(Directory.GetFiles(selectedFolderPath, "*" + extension));
                }
            }
        }

        private int currentUnshuffledIndex = 0;

        private void PlayNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Count > 0)
            {
                if (shuffledIndices != null && currentShuffleIndex < shuffledIndices.Count - 1)
                {
                    currentShuffleIndex++;
                    int nextIndex = shuffledIndices[currentShuffleIndex];
                    PlayMusicAtIndex(nextIndex);
                }
                else
                {
                    if (currentUnshuffledIndex < musicFiles.Count - 1)
                    {
                        currentUnshuffledIndex++;
                        PlayMusicAtIndex(currentUnshuffledIndex);
                    }
                    else
                    {
                        currentUnshuffledIndex = 0;
                        PlayMusicAtIndex(currentUnshuffledIndex);
                    }
                }
            }
        }

        private void PlayPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Count > 0)
            {
                if (shuffledIndices != null && currentShuffleIndex > 0)
                {
                    currentShuffleIndex--;
                    int previousIndex = shuffledIndices[currentShuffleIndex];
                    PlayMusicAtIndex(previousIndex);
                }
                else
                {
                    if (currentUnshuffledIndex > 0)
                    {
                        currentUnshuffledIndex--;
                        PlayMusicAtIndex(currentUnshuffledIndex);
                    }
                    else
                    {
                        currentUnshuffledIndex = musicFiles.Count - 1;
                        PlayMusicAtIndex(currentUnshuffledIndex);
                    }
                }
            }
        }





        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (reader != null && waveOut != null)
            {
                var newPosition = (long)(reader.Length * TimeSlider.Value);
                reader.Position = newPosition;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (waveOut != null)
            {
                waveOut.Volume = (float)VolumeSlider.Value;
                VolumeText.Text = $"{(int)(VolumeSlider.Value * 100)}%";
            }
        }

        private List<int> shuffledIndices;
        private int currentShuffleIndex = 0;

        private void ShuffleMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Count > 0)
            {
                if (shuffledIndices == null || currentShuffleIndex >= shuffledIndices.Count)
                {
                    shuffledIndices = Enumerable.Range(0, musicFiles.Count).OrderBy(x => Guid.NewGuid()).ToList();
                    currentShuffleIndex = 0;
                }

                if (currentShuffleIndex < shuffledIndices.Count)
                {
                    int nextIndex = shuffledIndices[currentShuffleIndex];
                    PlayMusicAtIndex(nextIndex);
                    currentShuffleIndex++;
                }
            }
        }
    }
}
