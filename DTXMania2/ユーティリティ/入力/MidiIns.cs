using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DTXMania2
{
    /// <summary>
    ///     すべてのMIDI入力デバイス。
    /// </summary>
    class MidiIns : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		デバイス名のリスト。インデックスはデバイス番号。
        /// </summary>
        public List<string> DeviceName { get; } = new List<string>();

        /// <summary>
        ///		FootPedal の MIDIコード。
        /// </summary>
        /// <remarks>
        ///		FootPedal 同時 HiHat キャンセル処理に使用される。
        ///		コードが判明次第、セットすること。
        /// </remarks>
        public List<int> FootPedalNotes { get; } = new List<int>();

        /// <summary>
        ///		HiHat (Open, Close, etc,.) のMIDIコード。
        /// </summary>
        /// <remarks>
        ///		FootPedal 同時 HiHat キャンセル処理に使用される。
        ///		コードが判明次第、セットすること。
        /// </remarks>
        public List<int> HiHatNotes { get; } = new List<int>();

        /// <summary>
        ///		入力イベントのリスト。
        ///		ポーリング時に、前回のポーリング（またはコンストラクタ）以降に発生した入力イベントが格納される。
        /// </summary>
        public List<InputEvent> 入力イベントリスト { get; protected set; } = new List<InputEvent>();



        // 生成と終了


        public MidiIns()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // コールバックをデリゲートとして生成し、そのデリゲートをGCの対象から外す。
            this._midiInProc = new MidiInProc( this.MIDI入力コールバック );
            this._midiInProcGCh = GCHandle.Alloc( this._midiInProc );

            // デバイス数を取得。
            uint MIDI入力デバイス数 = midiInGetNumDevs();
            Log.Info( $"MIDI入力デバイス数 = {MIDI入力デバイス数}" );

            // すべてのMIDI入力デバイスについて...
            for( uint id = 0; id < MIDI入力デバイス数; id++ )
            {
                // デバイス名を取得。
                var caps = new MIDIINCAPS();
                midiInGetDevCaps( id, ref caps, Marshal.SizeOf( caps ) );
                this.DeviceName.Add( caps.szPname );
                Log.Info( $"MidiIn[{id}]: {caps.szPname}" );

                // MIDI入力デバイスを開く。コールバックは全デバイスで共通。
                IntPtr hMidiIn = default;
                if( ( (uint) CSCore.MmResult.NoError == midiInOpen( ref hMidiIn, id, this._midiInProc, default, CALLBACK_FUNCTION ) ) && ( default != hMidiIn ) )
                {
                    this._MIDI入力デバイスハンドルリスト.Add( hMidiIn );
                    midiInStart( hMidiIn );
                }
            }
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // すべてのMIDI入力デバイスの受信を停止し、デバイスを閉じる。
            foreach( var hMidiIn in this._MIDI入力デバイスハンドルリスト )
            {
                midiInStop( hMidiIn );
                midiInReset( hMidiIn );
                midiInClose( hMidiIn );
            }
            this._MIDI入力デバイスハンドルリスト.Clear();

            // コールバックデリゲートをGCの対象に戻し、デリゲートへの参照を破棄する。
            lock( this._コールバック同期 )     // コールバックが実行中でないことを保証する。（不十分だが）
            {
                this._midiInProcGCh.Free();
                this._midiInProcGCh = default;
            }
        }



        // 入力


        public void ポーリングする()
        {
            // 前回のポーリングから今回までに蓄えたイベントをキャッシュへ参照渡し。
            // 蓄積用リストを新しく確保する。
            lock( this._コールバック同期 )
            {
                this.入力イベントリスト = this._蓄積用入力イベントリスト;
                this._蓄積用入力イベントリスト = new List<InputEvent>();
            }

            // FootPedal同時HHのキャンセル処理。
            if( ( 0 < this.FootPedalNotes.Count ) && ( 0 < this.HiHatNotes.Count ) )
            {
                #region " (1) FootPedalとほぼ同時にHiHatが鳴っていたら、そのHiHatに無効印（Key=-1）を付与する。"
                //-----------------
                for( int i = 0; i < this.入力イベントリスト.Count; i++ )
                {
                    // FP を探す。（まずHHより数は少ないだろう）
                    int fppos = this.FootPedalNotes.FindIndex( ( note ) => ( note == this.入力イベントリスト[ i ].Key ) );
                    if( 0 > fppos )
                        continue;

                    // FP の前後が HH 、かつ時刻がしきい値内なら、そのHHは消す。
                    const double しきい値 = 0.006;       // 6ms
                    int[] 前後pos = { fppos - 1, fppos + 1 };     // 最大１つまでしか消さないので、前後１つずつのチェックのみ行う。
                    foreach( int pos in 前後pos )
                    {
                        if( ( 0 <= pos ) && ( this.入力イベントリスト.Count > pos ) &&
                            ( 0 <= this.HiHatNotes.FindIndex( ( hh ) => ( hh == this.入力イベントリスト[ pos ].Key ) ) ) )
                        {
                            long 時刻差ct = Math.Abs( this.入力イベントリスト[ fppos ].TimeStamp - this.入力イベントリスト[ pos ].TimeStamp );
                            double 時刻差 = (double) 時刻差ct / (double) Stopwatch.Frequency;
                            if( しきい値 >= 時刻差 )
                            {
                                this.入力イベントリスト[ pos ].Key = -1;       // 無効印
                                break;  // 最大１つまでしか消さないので、これでチェックは終了。
                            }
                        }
                    }
                }
                //-----------------
                #endregion

                #region " (2) 無効印のあるイベントをすべて取り除く。"
                //-----------------
                this.入力イベントリスト.RemoveAll( ( ev ) => ( -1 == ev.Key ) );
                //-----------------
                #endregion
            }
        }

        public bool キーが押された( int deviceID, int key, out InputEvent? ev )
        {
            ev = this.入力イベントリスト.Find( ( e ) => ( e.DeviceID == deviceID && e.Key == key && e.押された ) );
            return ( null != ev );
        }

        public bool キーが押された( int deviceID, int key )
            => this.キーが押された( deviceID, key, out _ );

        public bool キーが押されている( int deviceID, int key )
            => false;   // 常に false

        public bool キーが離された( int deviceID, int key, out InputEvent? ev )
        {
            // MIDI入力では扱わない。
            ev = null;
            return false;
        }

        public bool キーが離された( int deviceID, int key )
            => this.キーが離された( deviceID, key, out _ );

        public bool キーが離されている( int deviceID, int key )
            => false;   // 常に false



        // ローカル


        protected virtual void MIDI入力コールバック( IntPtr hMidiIn, uint wMsg, int dwInstance, int dwParam1, int dwParam2 )
        {
            var timeStamp = Stopwatch.GetTimestamp();     // できるだけ早く取得しておく。

            if( MIM_DATA != wMsg )
                return;

            int deviceID = this._MIDI入力デバイスハンドルリスト.FindIndex( ( h ) => ( h == hMidiIn ) );
            if( 0 > deviceID )
                return;

            byte ch = (byte) ( dwParam1 & 0xFF );
            byte ev = (byte) ( dwParam1 & 0xF0 );
            byte p1 = (byte) ( ( dwParam1 >> 8 ) & 0xFF );
            byte p2 = (byte) ( ( dwParam1 >> 16 ) & 0xFF );

            if( ( 0x90 == ev ) && ( 0 != p2 ) )  // Velocity(p2)==0 を NoteOFF 扱いとする機器があるのでそれに倣う
            {
                #region " (A) Note ON "
                //----------------
                var ie = new InputEvent() {
                    DeviceID = deviceID,
                    Key = p1,
                    押された = true,
                    TimeStamp = timeStamp,
                    Control = 0,
                    Velocity = p2,
                    Extra = $"{ch:X2}{p1:X2}{p2:X2}",
                };

                lock( this._コールバック同期 )
                    this._蓄積用入力イベントリスト.Add( ie );
                //----------------
                #endregion
            }
            else if( ( 0x80 == ev ) || ( 0x90 == ev && 0 == p2 ) )  // NoteOnかつVelocity(p2)==0 を NoteOFF 扱いとする機器があるのでそれに倣う
            {
                #region " (B) Note OFF "
                //----------------
                var ie = new InputEvent() {
                    DeviceID = deviceID,
                    Key = p1,
                    押された = false,
                    TimeStamp = timeStamp,
                    Control = 0,
                    Velocity = p2,
                    Extra = $"{ch:X2}{p1:X2}{p2:X2}",
                };

                lock( this._コールバック同期 )
                    this._蓄積用入力イベントリスト.Add( ie );
                //----------------
                #endregion
            }
            else if( 0xB0 == ev )
            {
                #region " (C) コントロールチェンジ（フットペダル他） "
                //----------------
                var ie = new InputEvent() {
                    DeviceID = deviceID,
                    Key = 255,          // コントロールチェンジのキーは 255 とする。
                    押された = true,    // 押された扱い。
                    TimeStamp = timeStamp,
                    Control = p1,       // コントロールチェンジの番号はこっち。
                    Velocity = p2,      // コントロール値はこっち。
                    Extra = $"{ch:X2}{p1:X2}{p2:X2}",
                };

                lock( this._コールバック同期 )
                    this._蓄積用入力イベントリスト.Add( ie );
                //----------------
                #endregion
            }
        }


        private List<IntPtr> _MIDI入力デバイスハンドルリスト = new List<IntPtr>();

        /// <summary>
        ///     コールバック関数で蓄積され、ポーリング時にキャッシュへコピー＆クリアされる。
        /// </summary>
        private List<InputEvent> _蓄積用入力イベントリスト = new List<InputEvent>();

        /// <summary>
        ///     すべてのMIDI入力デバイスで共通のコールバックのデリゲートとGCHandleと本体メソッド。
        /// </summary>
        private MidiInProc _midiInProc;

        /// <summary>
        ///     <see cref="MIDI入力コールバック(IntPtr, uint, int, int, int)"/> の固定用ハンドル。
        /// </summary>
        private GCHandle _midiInProcGCh;

        private readonly object _コールバック同期 = new object();


        #region " Win32 "
        //-----------------
        private const int CALLBACK_FUNCTION = 0x00030000;
        private const uint MIM_DATA = 0x000003C3;

        private delegate void MidiInProc( IntPtr hMidiIn, uint wMsg, int dwInstance, int dwParam1, int dwParam2 );

        [DllImport( "winmm.dll" )]
        private static extern uint midiInGetNumDevs();

        [DllImport( "winmm.dll" )]
        private static extern uint midiInOpen( ref IntPtr phMidiIn, uint uDeviceID, MidiInProc dwCallback, IntPtr dwInstance, int fdwOpen );

        [DllImport( "winmm.dll" )]
        private static extern uint midiInStart( IntPtr hMidiIn );

        [DllImport( "winmm.dll" )]
        private static extern uint midiInStop( IntPtr hMidiIn );

        [DllImport( "winmm.dll" )]
        private static extern uint midiInReset( IntPtr hMidiIn );

        [DllImport( "winmm.dll" )]
        private static extern uint midiInClose( IntPtr hMidiIn );

        public struct MIDIINCAPS
        {
            public short wMid;
            public short wPid;
            public int vDriverVersion;
            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 32 )]
            public string szPname;
            public int dwSupport;
        }

        [DllImport( "winmm.dll" )]
        private static extern uint midiInGetDevCaps( uint uDeviceID, ref MIDIINCAPS caps, int cbMidiInCaps );
        //-----------------
        #endregion
    }
}
