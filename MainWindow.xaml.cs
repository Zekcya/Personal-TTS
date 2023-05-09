using System;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NAudio.Wave;

namespace MyTTS
{
    public partial class MainWindow : Window
    {
        private SpeechSynthesizer _speechSynthesizer;
        private DirectSoundOut _primaryAudioOutput;
        private DirectSoundOut _audioOutput;
        private MemoryStream _memoryStream;

        public MainWindow()
        {
            InitializeComponent();
            _speechSynthesizer = new SpeechSynthesizer();
            LoadAvailableVoices();
            LoadAvailableAudioOutputs();
            InputTextBox.KeyDown += InputTextBox_KeyDown;
            _speechSynthesizer.Rate = 0; // Range: -10 to 10 (0 = default rate)
            _speechSynthesizer.Volume = 100; // Range: 0 to 100 (100 = maximum volume)

        }

        private void LoadAvailableVoices()
        {
            foreach (var voice in _speechSynthesizer.GetInstalledVoices().Where(v => v.Enabled))
            {
                VoiceComboBox.Items.Add(voice.VoiceInfo.Name);
            }

            if (VoiceComboBox.Items.Count > 0)
            {
                VoiceComboBox.SelectedIndex = 0;
            }
        }

        private void LoadAvailableAudioOutputs()
        {
            foreach (var device in DirectSoundOut.Devices)
            {
                AudioOutputComboBox.Items.Add(device.Description);
            }

            if (AudioOutputComboBox.Items.Count > 0)
            {
                AudioOutputComboBox.SelectedIndex = 0;
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SpeakButton_Click(sender, e);
            }
        }

        private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            var playbackStoppedTcs = new TaskCompletionSource<bool>();

            string textToSpeak = InputTextBox.Text;
            if (!string.IsNullOrEmpty(textToSpeak))
            {
                _speechSynthesizer.SpeakAsyncCancelAll();

                if (_audioOutput != null)
                {
                    _audioOutput.Stop();
                    _audioOutput.Dispose();
                    _audioOutput = null;
                }

                if (_primaryAudioOutput != null)
                {
                    _primaryAudioOutput.Stop();
                    _primaryAudioOutput.Dispose();
                    _primaryAudioOutput = null;
                }

                if (_memoryStream != null)
                {
                    _memoryStream.Dispose();
                    _memoryStream = null;
                }

                var selectedDevice = DirectSoundOut.Devices.ElementAt(AudioOutputComboBox.SelectedIndex);
                _audioOutput = new DirectSoundOut(selectedDevice.Guid);

                _primaryAudioOutput = new DirectSoundOut(); // Use the default constructor for primary audio device

                _memoryStream = new MemoryStream();
                var formatInfo = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
                _speechSynthesizer.SetOutputToAudioStream(_memoryStream, formatInfo);

                await Task.Run(() => _speechSynthesizer.Speak(textToSpeak));

                _memoryStream.Position = 0;
                using (var waveStream = new RawSourceWaveStream(_memoryStream, new WaveFormat(formatInfo.SamplesPerSecond, formatInfo.BitsPerSample, formatInfo.ChannelCount)))
                {
                    var selectedDeviceWaveProvider = new MemoryStreamWaveProvider(_memoryStream, waveStream.WaveFormat);
                    var primaryDeviceWaveProvider = new MemoryStreamWaveProvider(DuplicateMemoryStream(_memoryStream), waveStream.WaveFormat);

                    _audioOutput.Init(selectedDeviceWaveProvider);
                    _primaryAudioOutput.Init(primaryDeviceWaveProvider);

                    _audioOutput.Play();
                    _primaryAudioOutput.Play();

                    _audioOutput.PlaybackStopped += (s, args) =>
                    {
                        playbackStoppedTcs.SetResult(true);
                    };
                }
            }

            await playbackStoppedTcs.Task;
            InputTextBox.Clear();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _speechSynthesizer.SpeakAsyncCancelAll();
            if (_audioOutput != null)
            {
                _audioOutput.Stop();
            }
            if (_primaryAudioOutput != null)
            {
                _primaryAudioOutput.Stop();
            }
        }

        private MemoryStream DuplicateMemoryStream(MemoryStream originalStream)
        {
            var duplicatedStream = new MemoryStream();
            originalStream.Position = 0;
            originalStream.CopyTo(duplicatedStream);
            originalStream.Position = 0;
            duplicatedStream.Position = 0;
            return duplicatedStream;
        }

        private void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedVoice = VoiceComboBox.SelectedItem.ToString();
            _speechSynthesizer.SelectVoice(selectedVoice);
        }

        private void AudioOutputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The audio output will be updated when the Speak button is clicked.
        }
    }
}