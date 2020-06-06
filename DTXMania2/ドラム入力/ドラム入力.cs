using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;
using SSTFormat.v004;

namespace DTXMania2
{
    class ドラム入力
    {

        // プロパティ


        /// <summary>
        ///		全デバイスの入力イベントをドラム入力イベントに変換し、それを集めたリスト。
        /// </summary>
        /// <remarks>
        ///		<see cref="すべての入力デバイスをポーリングする(bool)"/> の呼び出し時にクリアされ、
        ///		その時点における、前回のポーリング以降の入力イベントで再構築される。
        ///		ただし、キーバインディングにキーとして登録されていない入力イベントは含まれない。
        /// </remarks>
        public List<ドラム入力イベント> ポーリング結果 { get; }

        public WeakReference<KeyboardHID> Keybaord { get; }

        public WeakReference<GameControllersHID> GameControllers { get; }

        public WeakReference<MidiIns> MidiIn { get; }




        // 生成と終了


        public ドラム入力( KeyboardHID keyboard, GameControllersHID gameControllers, MidiIns midiIns, int 最大入力履歴数 = 32 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var config = Global.App.システム設定;
            this.Keybaord = new WeakReference<KeyboardHID>( keyboard );
            this.GameControllers = new WeakReference<GameControllersHID>( gameControllers );
            this.MidiIn = new WeakReference<MidiIns>( midiIns );
            this._最大入力履歴数 = 最大入力履歴数;

            this.ポーリング結果 = new List<ドラム入力イベント>();
            this._入力履歴 = new List<ドラム入力イベント>( this._最大入力履歴数 );

            // MIDI入力デバイスの可変IDへの対応を行う。
            if( 0 < midiIns.DeviceName.Count )
            {
                var デバイスリスト = new Dictionary<int, string>();    // <デバイスID, デバイス名>

                #region " (1) 先に列挙された実際のデバイスに合わせて、デバイスリスト（配列番号がデバイス番号）を作成する。"
                //----------------
                for( int i = 0; i < midiIns.DeviceName.Count; i++ )
                    デバイスリスト.Add( i, midiIns.DeviceName[ i ] );
                //----------------
                #endregion
                
                #region " (2) キーバインディングのデバイスリストとマージして、新しいデバイスリストを作成する。"
                //----------------
                foreach( var kvp in config.MIDIデバイス番号toデバイス名 )
                {
                    var キーバインディング側のデバイス名 = kvp.Value;

                    if( デバイスリスト.ContainsValue( キーバインディング側のデバイス名 ) )
                    {
                        // (A) 今回も存在しているデバイスなら、何もしない。
                    }
                    else
                    {
                        // (B) 今回は存在していないデバイスなら、末尾（＝未使用ID）に登録する。
                        デバイスリスト.Add( デバイスリスト.Count, キーバインディング側のデバイス名 );
                    }
                }
                //----------------
                #endregion
                
                #region " (3) キーバインディングのデバイスから新しいデバイスへ、キーのIDを付け直す。"
                //----------------
                var 中間バッファ = new Dictionary<SystemConfig.IdKey, ドラム入力種別>();

                foreach( var kvp in config.MIDItoドラム )
                {
                    var キーのデバイスID = kvp.Key.deviceId;

                    // キーバインディングのデバイス番号 から、デバイスリストのデバイス番号 へ付け替える。
                    if( config.MIDIデバイス番号toデバイス名.TryGetValue( キーのデバイスID, out string? キーのデバイス名 ) )
                    {
                        キーのデバイスID = デバイスリスト.First( ( kvp2 ) => ( kvp2.Value == キーのデバイス名 ) ).Key;    // マージしたので、必ず存在する。
                    }

                    中間バッファ.Add( new SystemConfig.IdKey( キーのデバイスID, kvp.Key.key ), kvp.Value );    // デバイスID以外は変更なし。
                }

                config.MIDItoドラム.Clear();

                for( int i = 0; i < 中間バッファ.Count; i++ )
                {
                    var kvp = 中間バッファ.ElementAt( i );
                    config.MIDItoドラム.Add( new SystemConfig.IdKey( kvp.Key.deviceId, kvp.Key.key ), kvp.Value );
                }
                //----------------
                #endregion
                
                #region " (4) 新しいデバイスリストをキーバインディングに格納して、保存する。"
                //----------------
                config.MIDIデバイス番号toデバイス名.Clear();

                for( int i = 0; i < デバイスリスト.Count; i++ )
                    config.MIDIデバイス番号toデバイス名.Add( i, デバイスリスト[ i ] );

                config.保存する();
                //----------------
                #endregion
            }
            else
            {
                // 列挙されたMIDI入力デバイスがまったくないなら、キーバインディングは何もいじらない。
            }
        }



        // 押下チェック


        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、指定したドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <param name="イベント">調べるドラム入力イベント。</param>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool ドラムが入力された( ドラム入力種別 drumType )
        {
            if( 0 == this.ポーリング結果.Count )   // 0 であることが大半だと思われるので、特別扱い。
            {
                return false;
            }
            else
            {
                return ( null != this.ポーリング結果.FirstOrDefault( ( ev ) => ( ev.Type == drumType && ev.InputEvent.押された ) ) );
            }
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、指定したドラム入力イベント集合のいずれか１つ以上が含まれているかを確認する。
        /// </summary>
        /// <param name="drumTypes">調べるドラム入力イベントの集合。</param>
        /// <returns><see cref="ポーリング結果"/>に、指定したドラム入力イベントのいずれか１つ以上が含まれていれば true。</returns>
        public bool ドラムのいずれか１つが入力された( IEnumerable<ドラム入力種別> drumTypes )
        {
            if( 0 == this.ポーリング結果.Count )   // 0 であることが大半だと思われるので、特別扱い。
            {
                return false;
            }
            else
            {
                return ( null != this.ポーリング結果.FirstOrDefault( ( ev ) => ( ev.InputEvent.押された && drumTypes.Contains( ev.Type ) ) ) );
            }
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、決定キーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool 確定キーが入力された()
        {
            return this.ドラムのいずれか１つが入力された(
                new[] {
                    ドラム入力種別.LeftCrash,
                    ドラム入力種別.RightCrash,
                    ドラム入力種別.China,
                    ドラム入力種別.Ride,
                    ドラム入力種別.Splash,
                } )
                ;// || this.Keyboard.キーが押された( 0, Key.Return );		Enter は、既定で LeftCrash に割り当てられている前提。
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、キャンセルキーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool キャンセルキーが入力された()
        {
            return
                this.Keybaord.TryGetTarget( out var keyboard ) &&
                keyboard.キーが押された( 0, System.Windows.Forms.Keys.Escape );
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、上移動キーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool 上移動キーが入力された()
        {
            return
                ( this.Keybaord.TryGetTarget( out var keyboard ) && keyboard.キーが押された( 0, System.Windows.Forms.Keys.Up ) ) ||
                this.ドラムのいずれか１つが入力された( new[] { ドラム入力種別.Tom1, ドラム入力種別.Tom1_Rim } );
        }
        public bool 上移動キーが押されている()
        {
            return
                this.Keybaord.TryGetTarget( out var keyboard ) &&
                keyboard.キーが押されている( 0, System.Windows.Forms.Keys.Up );
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、下移動キーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool 下移動キーが入力された()
        {
            return
                ( this.Keybaord.TryGetTarget( out var keyboard ) && keyboard.キーが押された( 0, System.Windows.Forms.Keys.Down ) ) ||
                this.ドラムのいずれか１つが入力された( new[] { ドラム入力種別.Tom2, ドラム入力種別.Tom2_Rim } );
        }
        public bool 下移動キーが押されている()
        {
            return
                this.Keybaord.TryGetTarget( out var keyboard ) &&
                keyboard.キーが押されている( 0, System.Windows.Forms.Keys.Down );
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、左移動キーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool 左移動キーが入力された()
        {
            return
                ( this.Keybaord.TryGetTarget( out var keyboard ) && keyboard.キーが押された( 0, System.Windows.Forms.Keys.Left ) ) ||
                this.ドラムのいずれか１つが入力された( new[] { ドラム入力種別.Snare, ドラム入力種別.Snare_ClosedRim, ドラム入力種別.Snare_OpenRim } );
        }
        public bool 左移動キーが押されている()
        {
            return
                this.Keybaord.TryGetTarget( out var keyboard ) &&
                keyboard.キーが押されている( 0, System.Windows.Forms.Keys.Left );
        }

        /// <summary>
        ///		現在の<see cref="ポーリング結果"/>に、右移動キーとみなせるドラム入力イベントが含まれているかを確認する。
        /// </summary>
        /// <returns><see cref="ポーリング結果"/>に含まれていれば true。</returns>
        public bool 右移動キーが入力された()
        {
            return
                ( this.Keybaord.TryGetTarget( out var keyboard ) && keyboard.キーが押された( 0, System.Windows.Forms.Keys.Right ) ) ||
                this.ドラムのいずれか１つが入力された( new[] { ドラム入力種別.Tom3, ドラム入力種別.Tom3_Rim } );
        }
        public bool 右移動キーが押されている()
        {
            return
                this.Keybaord.TryGetTarget( out var keyboard ) &&
                keyboard.キーが押されている( 0, System.Windows.Forms.Keys.Right );
        }

        /// <summary>
        ///		現在の履歴において、指定したシーケンスが成立しているかを確認する。
        /// </summary>
        /// <param name="シーケンス">	確認したいシーケンス。</param>
        /// <returns>シーケンスが成立しているなら true。</returns>
        /// <remarks>
        ///		指定したシーケンスが現在の履歴の一部に見られれば、成立しているとみなす。
        ///		履歴内に複数存在している場合は、一番 古 い シーケンスが対象となる。
        ///		成立した場合、そのシーケンスと、それより古い履歴はすべて削除される。
        /// </remarks>
        public bool シーケンスが入力された( IEnumerable<ドラム入力イベント> シーケンス )
        {
            int シーケンスのストローク数 = シーケンス.Count();       // ストローク ＝ ドラム入力イベント（シーケンスの構成単位）

            if( 0 == シーケンスのストローク数 )
                return false;   // 空シーケンスは常に不成立。

            if( this._入力履歴.Count < シーケンスのストローク数 )
                return false;   // 履歴数が足りない。

            int 履歴の検索開始位置 = this._入力履歴.IndexOf( シーケンス.ElementAt( 0 ) );

            if( -1 == 履歴の検索開始位置 )
                return false;   // 最初のストロークが見つからない。

            if( ( this._入力履歴.Count - 履歴の検索開始位置 ) < シーケンスのストローク数 )
                return false;   // 履歴数が足りない。

            // 検索開始位置から末尾へ、すべてのストロークが一致するか確認する。
            for( int i = 1; i < シーケンスのストローク数; i++ )
            {
                if( this._入力履歴[ 履歴の検索開始位置 + i ] != シーケンス.ElementAt( i ) )
                    return false;   // 一致しなかった。
            }

            // 見つけたシーケンスならびにそれより古い履歴を削除する。
            this._入力履歴.RemoveRange( 0, ( 履歴の検索開始位置 + シーケンスのストローク数 ) );

            return true;
        }

        /// <summary>
        ///		現在の履歴において、指定したシーケンスが成立しているかを確認する。
        /// </summary>
        /// <param name="シーケンス">確認したいシーケンス。</param>
        /// <returns>シーケンスが成立しているなら true。</returns>
        /// <remarks>
        ///		指定したシーケンスが現在の履歴の一部に見られれば、成立しているとみなす。
        ///		履歴内に複数存在している場合は、一番 古 い シーケンスが対象となる。
        ///		成立した場合、そのシーケンスと、それより古い履歴はすべて削除される。
        /// </remarks>
        public bool シーケンスが入力された( IEnumerable<ドラム入力種別> シーケンス )
        {
            // ストロークはシーケンスの構成単位。ここでは「指定されたドラム入力種別に対応するドラム入力イベント」と同義である。
            // ドラム入力種別 と ドラム入力イベント は、1 対 N の関係である。

            bool 適合する( ドラム入力種別 drumType, ドラム入力イベント drumEvent )
                => ( drumEvent.Type == drumType && drumEvent.InputEvent.押された );

            int シーケンスのストローク数 = シーケンス.Count();

            if( ( 0 == シーケンスのストローク数 ) ||                   // 空シーケンスは常に不成立。
                ( this._入力履歴.Count < シーケンスのストローク数 ) )   // 履歴数が足りない。
                return false;


            // 検索を開始する位置を特定する。

            int 履歴の検索開始位置 = this._入力履歴.FindIndex( ( e ) => 適合する( シーケンス.ElementAt( 0 ), e ) );

            if( -1 == 履歴の検索開始位置 )
                return false;   // 最初のストロークが見つからない。

            if( シーケンスのストローク数 > ( this._入力履歴.Count - 履歴の検索開始位置 ) )
                return false;   // 履歴数が足りない。


            // 検索開始位置から末尾へ向かって、すべてのストロークが一致するか確認する。

            for( int i = 1; i < シーケンスのストローク数; i++ )
            {
                if( !( 適合する( シーケンス.ElementAt( i ), this._入力履歴[ 履歴の検索開始位置 + i ] ) ) )
                    return false;   // 一致しなかった。
            }


            // すべて一致したので、そのシーケンスならびにそれより古い履歴を削除する。

            this._入力履歴.RemoveRange( 0, ( 履歴の検索開始位置 + シーケンスのストローク数 ) );

            return true;
        }

        /// <summary>
        ///		現在の履歴において、指定したシーケンスが成立しているかを確認する。
        /// </summary>
        /// <param name="シーケンス">確認したいシーケンス。</param>
        /// <returns>シーケンスが成立しているなら true。</returns>
        /// <remarks>
        ///		指定したシーケンスが現在の履歴の一部に見られれば、成立しているとみなす。
        ///		履歴内に複数存在している場合は、一番 古 い シーケンスが対象となる。
        ///		成立した場合、そのシーケンスと、それより古い履歴はすべて削除される。
        /// </remarks>
        public bool シーケンスが入力された( IEnumerable<レーン種別> シーケンス, 演奏.ドラムチッププロパティリスト ドラムチッププロパティリスト )
        {
            // ストロークはシーケンスの構成単位。ここでは、「指定されたレーン種別に対応するドラム入力種別に対応するドラム入力イベント」と同義。
            // レーン種別 と ドラム入力種別 と ドラム入力イベント は、N 対 M 対 P の関係である。

            bool 適合する( レーン種別 laneType, ドラム入力イベント drumEvent )
                => ( 0 < ドラムチッププロパティリスト.チップtoプロパティ.Count( ( kvp ) => ( ( kvp.Value.レーン種別 == laneType ) && ( kvp.Value.ドラム入力種別 == drumEvent.Type ) && ( drumEvent.InputEvent.押された ) ) ) );

            int シーケンスのストローク数 = シーケンス.Count();

            if( ( 0 == シーケンスのストローク数 ) ||                   // 空シーケンスは常に不成立。
                ( this._入力履歴.Count < シーケンスのストローク数 ) )   // 履歴数が足りない。
                return false;


            // 検索を開始する位置を特定する。

            int 履歴の検索開始位置 = this._入力履歴.FindIndex( ( e ) => 適合する( シーケンス.ElementAt( 0 ), e ) );

            if( -1 == 履歴の検索開始位置 )
                return false;   // 最初のストロークが見つからない。

            if( シーケンスのストローク数 > ( this._入力履歴.Count - 履歴の検索開始位置 ) )
                return false;   // 履歴数が足りない。


            // 検索開始位置から末尾へ向かって、すべてのストロークが一致するか確認する。

            for( int i = 1; i < シーケンスのストローク数; i++ )
            {
                if( !( 適合する( シーケンス.ElementAt( i ), this._入力履歴[ 履歴の検索開始位置 + i ] ) ) )
                    return false;   // 一致しなかった。
            }


            // すべて一致したので、そのシーケンスならびにそれより古い履歴を削除する。

            this._入力履歴.RemoveRange( 0, ( 履歴の検索開始位置 + シーケンスのストローク数 ) );

            return true;
        }



        // ポーリング


        /// <summary>
        ///		すべての入力デバイスをポーリングし、<see cref="ポーリング結果"/>のクリア＆再構築と、入力履歴の更新を行う。
        /// </summary>
        /// <param name="入力履歴を記録する">履歴に残す必要がないとき（演奏時の入力イベントなど）には false を指定する。</param>
        public void すべての入力デバイスをポーリングする( bool 入力履歴を記録する = true )
        {
            // 入力履歴が OFF から ON に変わった場合には、入力履歴を全クリアする。

            if( 入力履歴を記録する && this._入力履歴の記録を中断している )
            {
                this._入力履歴.Clear();
                this._前回の入力履歴の追加時刻sec = null;

            }
            this._入力履歴を記録中である = 入力履歴を記録する;


            // 個別にポーリングする。

            this.ポーリング結果.Clear();

            var config = Global.App.システム設定;
            {
                if( this.Keybaord.TryGetTarget( out var keyboard ) )
                {
                    keyboard.ポーリングする();
                    this._入力イベントを取得する( keyboard.入力イベントリスト, config.キーボードtoドラム, 入力履歴を記録する );
                }
                if( this.GameControllers.TryGetTarget( out var gameController ) )
                {
                    gameController.ポーリングする();
                    this._入力イベントを取得する( gameController.入力イベントリスト, config.ゲームコントローラtoドラム, 入力履歴を記録する );
                }
                if( this.MidiIn.TryGetTarget( out var midiIns ) )
                {
                    midiIns.ポーリングする();
                    this._入力イベントを取得する( midiIns.入力イベントリスト, config.MIDItoドラム, 入力履歴を記録する );
                }
            }


            // タイムスタンプの小さい順にソートする。

            this.ポーリング結果.Sort( ( x, y ) => (int) ( x.InputEvent.TimeStamp - y.InputEvent.TimeStamp ) );
        }


        /// <summary>
        ///		これまでにポーリングで取得された入力の履歴。
        /// </summary>
        /// <remarks>
        ///		キーバインディングに従ってマッピングされた後の、ドラム入力イベントを対象とする。
        ///		リストのサイズには制限があり（<see cref="_最大入力履歴数"/>）、それを超える場合は、ポーリング時に古いイベントから削除されていく。
        /// </remarks>
        private List<ドラム入力イベント> _入力履歴;

        private int _最大入力履歴数 = 32;

        private bool _入力履歴を記録中である = true;
        private bool _入力履歴の記録を中断している
        {
            get => !this._入力履歴を記録中である;
            set => this._入力履歴を記録中である = !value;
        }

        /// <summary>
        ///		null なら、入力履歴に追加された入力がまだないことを示す。
        /// </summary>
        private double? _前回の入力履歴の追加時刻sec = null;



        // ローカル


        /// <summary>
        ///		単一の IInputDevice をポーリングし、対応表に従ってドラム入力へ変換して、ポーリング結果 に追加登録する。
        /// </summary>
        private void _入力イベントを取得する( IReadOnlyList<InputEvent> 入力イベントリスト, IReadOnlyDictionary<SystemConfig.IdKey, ドラム入力種別> デバイスtoドラム対応表, bool 入力履歴を記録する )
        {
            // ポーリングされた入力イベントのうち、キーバインディングに登録されているイベントだけを ポーリング結果 に追加する。

            foreach( var ev in 入力イベントリスト )
            {
                // キーバインディングを使って、入力イベント ev をドラム入力 evKey にマッピングする。
                var evKey = new SystemConfig.IdKey( ev );
                if( false == デバイスtoドラム対応表.ContainsKey( evKey ) )
                    continue;   // 使われないならスキップ。

                // ドラム入力を、ポーリング結果に追加登録する。
                var ドラム入力 = new ドラム入力イベント( ev, デバイスtoドラム対応表[ evKey ] );
                this.ポーリング結果.Add( ドラム入力 );

                // ドラム入力を入力履歴に追加登録する。
                if( 入力履歴を記録する &&
                    ev.押された &&
                    ドラム入力.Type != ドラム入力種別.HiHat_Control )   // HHC は入力履歴の対象外とする。
                {
                    const double _連続入力だとみなす最大の間隔sec = 0.5;

                    double 入力時刻sec = ev.TimeStamp;

                    // 容量がいっぱいなら、古い履歴から削除する。
                    if( this._入力履歴.Count >= this._最大入力履歴数 )
                        this._入力履歴.RemoveRange( 0, ( this._入力履歴.Count - this._最大入力履歴数 + 1 ) );

                    // 前回の追加登録時刻からの経過時間がオーバーしているなら、履歴をすべて破棄する。
                    if( null != this._前回の入力履歴の追加時刻sec )
                    {
                        var 前回の登録からの経過時間sec = 入力時刻sec - this._前回の入力履歴の追加時刻sec;

                        if( _連続入力だとみなす最大の間隔sec < 前回の登録からの経過時間sec )
                            this._入力履歴.Clear();
                    }

                    // 今回の入力を履歴に登録する。
                    this._入力履歴.Add( ドラム入力 );
                    this._前回の入力履歴の追加時刻sec = 入力時刻sec;
                }
            }
        }
    }
}
