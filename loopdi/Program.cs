using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;

namespace loopdi
{
    using CSCore.Codecs;
    using CSCore.SoundOut;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading;
    using Terminal.Gui;

    class Demo : IDisposable
    {
        private List<SerialDisposable> state = Enumerable.Range(0,10).Select(i=> new SerialDisposable()).ToList();

        public IObservable<Unit> PlayASound(string filename)
        {

            return Observable.Create<Unit>(observer =>
            {
                //Contains the sound to play
                IWaveSource soundSource = GetSoundSource(filename);
                LoopStream ls = new LoopStream(soundSource);
                //SoundOut implementation which plays the sound
                ISoundOut soundOut = GetSoundOut();
                //Tell the SoundOut which sound it has to play
                soundOut.Initialize(ls);
                //Play the sound
                soundOut.Play();

                return Disposable.Create(() =>
                {
                    //Stop the playback
                    soundOut.Stop();
                    soundOut.Dispose();
                    soundSource.Dispose();
                });
            });



        }


        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        private IWaveSource GetSoundSource(string filename)
        {
            // Instead of using the CodecFactory as helper, you specify the decoder directly:
            var codec = CodecFactory.Instance.GetCodec(filename);
            return codec;
        }

        void Record(int channel)
        {
            state[channel].Disposable = Disposable.Empty;

            //create a new soundIn instance
            ISoundIn soundIn = new WasapiCapture();
            //optional: set some properties 
            //soundIn.Device = ...
            //...

            //initialize the soundIn instance
            soundIn.Initialize();

            //create a SoundSource around the the soundIn instance
            //this SoundSource will provide data, captured by the soundIn instance
            var soundInSource = new SoundInSource( soundIn ) {FillWithZeros = false};

            //create a source, that converts the data provided by the
            //soundInSource to any other format
            //in this case the "Fluent"-extension methods are being used
            var convertedSource = soundInSource
                                 .ToStereo()               //2 channels (for example)
                                 .ChangeSampleRate( 8000 ) // 8kHz sample rate
                                 .ToSampleSource()
                                 .ToWaveSource( 16 ); //16 bit pcm

            //register an event handler for the DataAvailable event of 
            //the soundInSource
            //Important: use the DataAvailable of the SoundInSource
            //If you use the DataAvailable event of the ISoundIn itself
            //the data recorded by that event might won't be available at the
            //soundInSource yet


            var waveWriter = new WaveWriter(channelFile(channel), convertedSource.WaveFormat);

            soundInSource.DataAvailable += ( s, e ) =>
            {
                //read data from the converedSource
                //important: don't use the e.Data here
                //the e.Data contains the raw data provided by the 
                //soundInSource which won't have your target format
                var buffer = new byte[convertedSource.WaveFormat.BytesPerSecond / 2];
                int read;

                //keep reading as long as we still get some data
                //if you're using such a loop, make sure that soundInSource.FillWithZeros is set to false
                while ((read = convertedSource.Read( buffer, 0, buffer.Length )) > 0)
                {
                    //your logic follows here
                    //for example: stream.Write(buffer, 0, read);
                    waveWriter.Write( buffer,0, read );
                }
            };

            //we've set everything we need -> start capturing data
            soundIn.Start();
            state[channel].Disposable = Disposable.Create( () =>
            {
                soundIn.Stop();
                waveWriter.Dispose();
            } );
        }

        void Stop(int channel)
        {
            state[channel].Disposable = Disposable.Empty;
        }

        void Play(int channel)
        {
            state[channel].Disposable = Disposable.Empty;
            state[channel].Disposable = PlayASound(channelFile(channel)).Repeat().Subscribe();
        }

        private static string channelFile(int channel)
        {
            return $"{channel}test.wav";
        }

        public void Start()
        {
            Application.Init();
            var top = Application.Top;

            // Creates the top-level window to show
            var win = new Window( new Rect( 0, 1, top.Frame.Width, top.Frame.Height - 1 ), "LoopDi" );
            top.Add( win );


            var text = "[0-9]Channel Select\n(R)ecord\n(P)lay\n(S)top";
            Console.WriteLine(text);
            ConsoleKeyInfo keyinfo;
            var currentChannel = 0;
            do
            {
                keyinfo = Console.ReadKey(true);
                switch (keyinfo.KeyChar)
                {
                    case 'r':
                        Record(currentChannel);
                        continue;
                    case 'p':
                        Play(currentChannel);
                        continue;
                    case 's':
                        Stop(currentChannel);
                        continue;
                }
                if (keyinfo.KeyChar >= '0' || keyinfo.KeyChar <= 9)
                {
                    currentChannel = (int)(keyinfo.KeyChar - '0');
                }
            } while (true);
        }

        public void Dispose()
        {
            state.ForEach(c=>c.Dispose());
        }
    }

    internal class Program
    {
        private static void Main( string[] args )
        {
            var app = new Demo();
            app.Start();
        }
    }
}
