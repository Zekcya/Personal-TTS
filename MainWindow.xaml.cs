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
        // Declare SpeechSynthesizer to generate speech
        private SpeechSynthesizer _speechSynthesizer;
        // Declare DirectSoundOut for controlling audio output
        private DirectSoundOut _primaryAudioOutput;
        private DirectSoundOut _audioOutput;
        // Declare MemoryStream to hold the generated speech audio
        private MemoryStream _memoryStream;

        public MainWindow()
        {
            InitializeComponent();
            // Initialize the SpeechSynthesizer
            _speechSynthesizer = new SpeechSynthesizer();
            // Load the available voices and audio outputs
            LoadAvailableVoices();
            LoadAvailableAudioOutputs();
            // Attach a KeyDown event to the InputTextBox
            InputTextBox.KeyDown += InputTextBox_KeyDown;
            // Set the default speech rate and volume
            _speechSynthesizer.Rate = 0; // Range: -10 to 10 (0 = default rate)
            _speechSynthesizer.Volume = 100; // Range: 0 to 100 (100 = maximum volume)
        }

        private void LoadAvailableVoices()
        {
            // Get all the available and enabled voices and add them to the ComboBox
            foreach (var voice in _speechSynthesizer.GetInstalledVoices().Where(v => v.Enabled))
            {
                VoiceComboBox.Items.Add(voice.VoiceInfo.Name);
            }

            // Select the first voice in the ComboBox by default
            if (VoiceComboBox.Items.Count > 0)
            {
                VoiceComboBox.SelectedIndex = 0;
            }
        }

        private void LoadAvailableAudioOutputs()
        {
            // Get all the available audio output devices and add them to the ComboBox
            foreach (var device in DirectSoundOut.Devices)
            {
                AudioOutputComboBox.Items.Add(device.Description);
            }

            // Select the first audio output in the ComboBox by default
            if (AudioOutputComboBox.Items.Count > 0)
            {
                AudioOutputComboBox.SelectedIndex = 0;
            }
        }

        // If Enter key is pressed in the InputTextBox, initiate the speech synthesis
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SpeakButton_Click(sender, e);
            }
        }

        // When the Speak button is clicked
        private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            // A task completion source is used to await until the playback is stopped
            var playbackStoppedTcs = new TaskCompletionSource<bool>();

            string textToSpeak = InputTextBox.Text;
            // If there's text to speak
            if (!string.IsNullOrEmpty(textToSpeak))
            {
                // Cancel any ongoing speech synthesis
                _speechSynthesizer.SpeakAsyncCancelAll();

                // Stop and dispose any ongoing audio output
                if (_audioOutput != null)
                {
                    _audioOutput.Stop();
                    _audioOutput.Dispose();
                    _audioOutput = null;
                }

                // Stop and dispose the primary audio output
                if (_primaryAudioOutput != null)
                {
                    _primaryAudioOutput.Stop();
                    _primaryAudioOutput.Dispose();
                    _primaryAudioOutput = null;
                }

                // Dispose the MemoryStream if it has been used before
                if (_memoryStream != null)
                {
                    _memoryStream.Dispose();
                    _memoryStream = null;
                }

                // Create a new DirectSoundOut instance for the selected audio output device
                var selectedDevice = DirectSoundOut.Devices.ElementAt(AudioOutputComboBox.SelectedIndex);
                _audioOutput = new DirectSoundOut(selectedDevice.Guid);

                // Create a new DirectSoundOut instance for the primary audio device
                _primaryAudioOutput = new DirectSoundOut();

                _memoryStream = new MemoryStream();
                var formatInfo = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
                // Set the output of the SpeechSynthesizer to the MemoryStream
                _speechSynthesizer.SetOutputToAudioStream(_memoryStream, formatInfo);

                // Generate the speech asynchronously
                await Task.Run(() => _speechSynthesizer.Speak(textToSpeak));

                _memoryStream.Position = 0;
                using (var waveStream = new RawSourceWaveStream(_memoryStream, new WaveFormat(formatInfo.SamplesPerSecond, formatInfo.BitsPerSample, formatInfo.ChannelCount)))
                {
                    var selectedDeviceWaveProvider = new MemoryStreamWaveProvider(_memoryStream, waveStream.WaveFormat);
                    var primaryDeviceWaveProvider = new MemoryStreamWaveProvider(DuplicateMemoryStream(_memoryStream), waveStream.WaveFormat);

                    // Initialize the audio outputs
                    _audioOutput.Init(selectedDeviceWaveProvider);
                    _primaryAudioOutput.Init(primaryDeviceWaveProvider);

                    // Start playing the speech audio on both audio outputs
                    _audioOutput.Play();
                    _primaryAudioOutput.Play();

                    // When the playback stops, set the TaskCompletionSource result to true
                    _audioOutput.PlaybackStopped += (s, args) =>
                    {
                        playbackStoppedTcs.SetResult(true);
                    };
                }
            }

            // Await until the playback stops
            await playbackStoppedTcs.Task;
            // Clear the input textbox
            InputTextBox.Clear();
        }

        // When the Stop button is clicked
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Cancel any ongoing speech synthesis
            _speechSynthesizer.SpeakAsyncCancelAll();
            // Stop any ongoing audio output
            if (_audioOutput != null)
            {
                _audioOutput.Stop();
            }
            // Stop the primary audio output
            if (_primaryAudioOutput != null)
            {
                _primaryAudioOutput.Stop();
            }
        }

        // This method duplicates a MemoryStream
        private MemoryStream DuplicateMemoryStream(MemoryStream originalStream)
        {
            var duplicatedStream = new MemoryStream();
            originalStream.Position = 0;
            originalStream.CopyTo(duplicatedStream);
            originalStream.Position = 0;
            duplicatedStream.Position = 0;
            return duplicatedStream;
        }

        // When a voice is selected in the ComboBox
        private void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedVoice = VoiceComboBox.SelectedItem.ToString();
            // Select the chosen voice in the SpeechSynthesizer
            _speechSynthesizer.SelectVoice(selectedVoice);
        }

        // When an audio output device is selected in the ComboBox
        private void AudioOutputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The audio output will be updated when the Speak button is clicked.
        }
    }
}

