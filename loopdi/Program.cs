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
    using Terminal.Gui;

    class Demo : IDisposable
    {
        private SerialDisposable state = new SerialDisposable();

        void Record()
        {

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


            var waveWriter = new WaveWriter("test.wav", convertedSource.WaveFormat);

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
            state.Disposable = Disposable.Create( () =>
            {
                soundIn.Stop();
                waveWriter.Dispose();
            } );
        }

        void Stop()
        {
            state.Disposable = Disposable.Empty;
        }

        public void Start()
        {
            Application.Init();
            var top = Application.Top;

            // Creates the top-level window to show
            var win = new Window( new Rect( 0, 1, top.Frame.Width, top.Frame.Height - 1 ), "LoopDi" );
            top.Add( win );


            win.Add(new Accelerator(0, 0, "(R)ecord", (Key)'r', Record));
            win.Add(new Accelerator(0, 1, "(S)top", (Key)'s', Stop));



            Application.Run();
        }

        public void Dispose()
        {
            state.Dispose();
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
