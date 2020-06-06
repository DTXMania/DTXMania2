using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;

namespace FDK
{
    /// <summary>
    ///     <see cref="ISampleSource"/> オブジェクトの生成を行う。
    /// </summary>
    public static class SampleSourceFactory
    {
        /// <summary>
        ///		指定されたファイルの音声をデコードし、<see cref="ISampleSource"/> オブジェクトを返す。
        ///		失敗すれば null 。
        /// </summary>
        public static ISampleSource? Create( SoundDevice device, VariablePath path, double 再生速度 = 1.0 )
        {
            if( !( File.Exists( path.変数なしパス ) ) )
            {
                Log.ERROR( $"ファイルが存在しません。[{path.変数付きパス}]" );
                return null;
            }

            var 拡張子 = Path.GetExtension( path.変数なしパス ).ToLower();

            if( ".ogg" == 拡張子 )
            {
                #region " OggVorvis "
                //----------------
                try
                {
                    // ファイルを読み込んで IWaveSource を生成。
                    using var audioStream = new FileStream( path.変数なしパス, FileMode.Open, FileAccess.Read );
                    using var waveSource = new NVorbisOnStreamingSampleSource( audioStream, device.WaveFormat ).ToWaveSource();

                    // IWaveSource をリサンプルして ISampleSource を生成。
                    return new ResampledOnMemoryWaveSource( waveSource, device.WaveFormat, 再生速度 ).ToSampleSource();
                }
                catch
                {
                    // ダメだったので次へ。
                }
                //----------------
                #endregion
            }
            if( ".wav" == 拡張子 )
            {
                #region " WAV "
                //----------------
                try
                {
                    // ファイルを読み込んで IWaveSource を生成。
                    using var waveSource = new WavOnStreamingWaveSource( path, device.WaveFormat );

                    if( waveSource.WaveFormat.WaveFormatTag == AudioEncoding.Pcm )  // ここでは PCM WAV のみサポート
                    {
                        // IWaveSource をリサンプルして ISampleSource を生成。
                        return new ResampledOnMemoryWaveSource( waveSource, device.WaveFormat, 再生速度 ).ToSampleSource();
                    }

                    // PCM WAV 以外は次へ。
                }
                catch
                {
                    // ダメだったので次へ。
                }
                //----------------
                #endregion
            }
            if( ".xa" == 拡張子 )
            {
                #region " XA "
                //----------------
                try
                {
                    // ファイルを読み込んで IWaveSource を生成。
                    using var waveSource = new XAOnMemoryWaveSource( path, device.WaveFormat );

                    // IWaveSource をリサンプルして ISampleSource を生成。
                    return new ResampledOnMemoryWaveSource( waveSource, device.WaveFormat, 再生速度 ).ToSampleSource();
                }
                catch
                {
                    // ダメだったので次へ。
                }
                //----------------
                #endregion
            }

            #region " 全部ダメだったら MediaFoundation で試みる。"
            //----------------
            try
            {
                // ファイルを読み込んで IWaveSource を生成。
                using var waveSource = new MediaFoundationOnMemoryWaveSource( path, device.WaveFormat );

                // デコード完了まで待機。
                if( !waveSource.DecodeComplete.Wait( 60_1000 ) ) // 最大1分
                    throw new TimeoutException( $"デコードタスクがタイムアウトしました。[{path.変数付きパス}]" );

                // IWaveSource をリサンプルして ISampleSource を生成。
                return new ResampledOnMemoryWaveSource( waveSource, device.WaveFormat, 再生速度 ).ToSampleSource();
            }
            catch
            {
                // ダメだった
            }
            //----------------
            #endregion

            Log.ERROR( $"未対応のオーディオファイルです。{path.変数付きパス}" );
            return null;
        }
    }
}

