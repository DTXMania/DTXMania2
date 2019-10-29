using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DTXMania2.オプション設定
{
    public partial class 入力割り当てダイアログ : Form
    {

        // 生成と終了


        public 入力割り当てダイアログ()
        {
            InitializeComponent();
        }

        public void 表示する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            using var timer = new Timer();

            #region " 設定値で初期化。"
            //----------------
            foreach( ドラム入力種別? drum in Enum.GetValues( typeof( ドラム入力種別 ) ) )
            {
                if( !drum.HasValue ||
                    drum == ドラム入力種別.Unknown ||
                    drum == ドラム入力種別.HiHat_Control )
                    continue;   // 除外（設定変更不可）

                this.comboBoxパッドリスト.Items.Add( drum.Value.ToString() );
            }

            // 変更後のキーバインディングを、現在の設定値で初期化。
            this._変更後のシステム設定 = Global.App.システム設定.Clone();

            // 最初のパッドを選択し、割り当て済みリストを更新。
            this.comboBoxパッドリスト.SelectedIndex = 0;

            // その他の初期化。
            this._前回の入力リスト追加時刻 = QPCTimer.生カウント相対値を秒へ変換して返す( QPCTimer.生カウント );
            this._FootPedal現在値 = 0;
            this.textBoxFootPedal現在値.Text = "0";
            this.textBoxFootPedal最小値.Text = this._変更後のシステム設定.FootPedal最小値.ToString();
            this.textBoxFootPedal最大値.Text = this._変更後のシステム設定.FootPedal最大値.ToString();
            this._変更あり = false;

            // 初期メッセージを出力。
            this.listView入力リスト.Items.Add( $"HID Keyboard の受付を開始しました。" );
            for( int i = 0; i < Global.AppForm.MidiIns.DeviceName.Count; i++ )
                this.listView入力リスト.Items.Add( $"MIDI IN [{i}] '{Global.AppForm.MidiIns.DeviceName[ i ]}' の受付を開始しました。" );
            this.listView入力リスト.Items.Add( "" );
            this.listView入力リスト.Items.Add( "* タイミングクロック信号、アクティブ信号は無視します。" );
            this.listView入力リスト.Items.Add( "* 入力と入力の間が500ミリ秒以上開いた場合は、間に空行を表示します。" );
            this.listView入力リスト.Items.Add( "" );
            this.listView入力リスト.Items.Add( "キーボードまたはMIDI信号を入力してください。" );
            //----------------
            #endregion

            // タイマーイベントを使って、定期的に、ゲームコントローラ/MIDI入力値の表示と、MIDIフットペダル開度ゲージの描画を行う。
            timer.Interval = 100;
            timer.Tick += ( sender, arg ) => {

                #region " ゲームコントローラをポーリングし、入力値を入力リストへ出力。"
                //----------------
                Global.AppForm.GameControllersHID.ポーリングする();

                for( int i = 0; i < Global.AppForm.GameControllersHID.入力イベントリスト.Count; i++ )
                {
                    var inputEvent = Global.AppForm.GameControllersHID.入力イベントリスト[ i ];

                    if( inputEvent.押された )
                    {
                        // 入力リストに表示。
                        var item = new ListViewItem入力リスト用( InputDeviceType.GameController, inputEvent );

                        // 既に割り当てられていたらそのドラム種別を表示。
                        var drumType = this._変更後のシステム設定.ゲームコントローラtoドラム
                            .Where( ( kvp ) => ( kvp.Key.deviceId == item.inputEvent.DeviceID && kvp.Key.key == item.inputEvent.Key ) )
                            .Select( ( kvp ) => kvp.Value );
                        if( 0 < drumType.Count() )
                            item.Text += $" （現在の割り当て: {drumType.ElementAt( 0 )}）";

                        this._一定時間が経っていれば空行を挿入する();

                        this.listView入力リスト.Items.Add( item );
                        this.listView入力リスト.EnsureVisible( this.listView入力リスト.Items.Count - 1 );
                    }
                }
                //----------------
                #endregion

                #region " MIDI入力をポーリングし、入力値を入力リストへ出力。"
                //----------------
                // MidiInChecker の機能もかねて、NoteOFF や ControlChange も表示する。（割り当てはできない。）

                Global.AppForm.MidiIns.ポーリングする();

                for( int i = 0; i < Global.AppForm.MidiIns.入力イベントリスト.Count; i++ )
                {
                    var inputEvent = Global.AppForm.MidiIns.入力イベントリスト[ i ];

                    if( inputEvent.押された && ( 255 == inputEvent.Key ) && ( 4 == inputEvent.Control ) )
                    {
                        #region " (A) フットペダルコントロールの場合　→　入力リストではなく専用のUIで表示。"
                        //----------------
                        if( this._FootPedal現在値 != inputEvent.Velocity )
                        {
                            // 現在値
                            this._FootPedal現在値 = inputEvent.Velocity;
                            this.textBoxFootPedal現在値.Text = this._FootPedal現在値.ToString();

                            // 最大値
                            if( this._FootPedal現在値 > this._変更後のシステム設定.FootPedal最大値 )
                            {
                                this._変更後のシステム設定.FootPedal最大値 = this._FootPedal現在値;
                                this.textBoxFootPedal最大値.Text = this._変更後のシステム設定.FootPedal最大値.ToString();
                            }

                            // 最小値
                            if( this._FootPedal現在値 <= this._変更後のシステム設定.FootPedal最小値 )
                            {
                                this._変更後のシステム設定.FootPedal最小値 = this._FootPedal現在値;
                                this.textBoxFootPedal最小値.Text = this._変更後のシステム設定.FootPedal最小値.ToString();
                            }
                        }
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " (B) その他のMIDI入出力　→　入力リストに表示。"
                        //----------------
                        var item = new ListViewItem入力リスト用( InputDeviceType.MidiIn, inputEvent );

                        // 既に割り当てられていたらそのドラム種別を表示。
                        var drumType = this._変更後のシステム設定.MIDItoドラム
                            .Where( ( kvp ) => ( kvp.Key.deviceId == item.inputEvent.DeviceID && kvp.Key.key == item.inputEvent.Key ) )
                            .Select( ( kvp ) => kvp.Value );
                        if( 0 < drumType.Count() )
                            item.Text += $" （現在の割り当て: {drumType.ElementAt( 0 )}）";

                        this._一定時間が経っていれば空行を挿入する();

                        this.listView入力リスト.Items.Add( item );
                        this.listView入力リスト.EnsureVisible( this.listView入力リスト.Items.Count - 1 );
                        //----------------
                        #endregion
                    }
                }
                //----------------
                #endregion

                #region " MIDIフットペダルの開度ゲージを描画。"
                //----------------
                using( var g = pictureBoxFootPedal.CreateGraphics() )
                {
                    var 全体矩形 = pictureBoxFootPedal.ClientRectangle;
                    var 背景色 = new System.Drawing.SolidBrush( pictureBoxFootPedal.BackColor );
                    var 最大値ゲージ色 = System.Drawing.Brushes.LightBlue;
                    var ゲージ色 = System.Drawing.Brushes.Blue;

                    g.FillRectangle( 背景色, 全体矩形 );

                    int 最大値用差分 = (int) ( 全体矩形.Height * ( 1.0 - this._変更後のシステム設定.FootPedal最大値 / 127.0 ) );
                    var 最大値ゲージ矩形 = new System.Drawing.Rectangle(
                        全体矩形.X,
                        全体矩形.Y + 最大値用差分,
                        全体矩形.Width,
                        全体矩形.Height - 最大値用差分 );
                    g.FillRectangle( 最大値ゲージ色, 最大値ゲージ矩形 );

                    int 現在値用差分 = (int) ( 全体矩形.Height * ( 1.0 - this._FootPedal現在値 / 127.0 ) );
                    var ゲージ矩形 = new System.Drawing.Rectangle(
                        全体矩形.X,
                        全体矩形.Y + 現在値用差分,
                        全体矩形.Width,
                        全体矩形.Height - 現在値用差分 );
                    g.FillRectangle( ゲージ色, ゲージ矩形 );
                }
                //----------------
                #endregion
            };

            this.KeyPreview = true; // Control への入力を先に Form が受け取れるようにする。

            this.KeyDown += ( sender, arg ) => {    // ダイアログで RawInput を使う方法が分からないので KeyDown を使う

                #region " キーボードの入力値を入力リストへ出力。"
                //----------------
                var inputEvent = new InputEvent() {
                    DeviceID = 0,
                    Key = (int) arg.KeyCode,
                    TimeStamp = 0,
                    Velocity = 100,
                    押された = true,
                    Control = 0,
                };

                var item = new ListViewItem入力リスト用( InputDeviceType.Keyboard, inputEvent );

                if( inputEvent.Key == (int) Keys.Escape )    // 割り当てされてほしくないキーはここへ。
                {
                    item.割り当て可能 = false;
                }

                // 既に割り当てられていたらそのドラム種別を表示。
                var drumType = this._変更後のシステム設定.キーボードtoドラム
                    .Where( ( kvp ) => ( kvp.Key.deviceId == item.inputEvent.DeviceID && kvp.Key.key == item.inputEvent.Key ) )
                    .Select( ( kvp ) => kvp.Value );

                if( 0 < drumType.Count() )
                    item.Text += $" （現在の割り当て: {drumType.ElementAt( 0 )}）";

                this._一定時間が経っていれば空行を挿入する();

                this.listView入力リスト.Items.Add( item );
                this.listView入力リスト.EnsureVisible( this.listView入力リスト.Items.Count - 1 );
                //----------------
                #endregion
            };

            timer.Start();

            #region " ダイアログを表示。"
            //----------------
            Cursor.Show();

            var dr = this.ShowDialog( Global.AppForm );

            if( Global.AppForm.ScreenMode.IsFullscreenMode )
                Cursor.Hide();
            //----------------
            #endregion

            timer.Stop();

            if( dr == DialogResult.OK )
            {
                // 設定値を反映する。
                Global.App.システム設定 = this._変更後のシステム設定.Clone();
                Global.App.システム設定.保存する();
            }
        }



        // ローカル


        private enum InputDeviceType
        {
            Keyboard,
            Mouse,
            GameController,
            MidiIn,
            Unknown
        }
        
        /// <summary>
        ///     <see cref="listView入力リスト"/> 用の ListViewItem 拡張クラス。
        ///     表示テキストのほかに、入力情報も持つ。
        /// </summary>
        private class ListViewItem入力リスト用 : ListViewItem
        {
            public bool 割り当て可能;
            public InputDeviceType deviceType;  // Device種別
            public InputEvent inputEvent;       // DeviceID, key, velocity

            public ListViewItem入力リスト用( InputDeviceType deviceType, InputEvent inputEvent )
            {
                this.割り当て可能 = true;
                this.deviceType = deviceType;
                this.inputEvent = inputEvent;

                switch( deviceType )
                {
                    case InputDeviceType.Keyboard:
                        this.Text = $"Keyboard, {inputEvent.Key}, '{( (Keys) inputEvent.Key ).ToString()}'";
                        break;

                    case InputDeviceType.GameController:
                        this.Text = $"GamePad, 0x{inputEvent.Key:X8}, '{HID.GetUsageName( (uint) inputEvent.Key )}'";
                        break;

                    case InputDeviceType.MidiIn:
                        if( inputEvent.押された )
                        {
                            if( 255 != inputEvent.Key )
                            {
                                this.Text = $"MidiIn[{inputEvent.DeviceID}], {inputEvent.Extra}, ノートオン, Note={inputEvent.Key}, Velocity={inputEvent.Velocity}";
                                this.割り当て可能 = true;                       // 割り当て可
                                this.ForeColor = System.Drawing.Color.Black;    // 黒
                            }
                            else
                            {
                                // フットペダル
                                this.Text = $"MidiIn[{inputEvent.DeviceID}], {inputEvent.Extra}, コントロールチェンジ, Control={inputEvent.Control}(0x{inputEvent.Control:X2}), Value={inputEvent.Velocity}";
                                this.割り当て可能 = false;                      // 割り当て不可
                                this.ForeColor = System.Drawing.Color.Green;    // 緑
                            }
                        }
                        else if( inputEvent.離された )
                        {
                            this.Text = $"MidiIn[{inputEvent.DeviceID}], {inputEvent.Extra}, ノートオフ, Note={inputEvent.Key}, Velocity={inputEvent.Velocity}";
                            this.割り当て可能 = false;                          // 割り当て不可
                            this.ForeColor = System.Drawing.Color.Gray;         // 灰
                        }
                        break;

                    default:
                        throw new ArgumentException( "未対応のデバイスです。" );
                }
            }
        }

        /// <summary>
        ///     <see cref="listView割り当て済み入力リスト"/> 用の ListViewItem 拡張クラス。
        ///     表示テキストのほかに、入力情報も持つ。
        /// </summary>
        private class ListViewItem割り当て済み入力リスト用 : ListViewItem
        {
            public bool 割り当て可能;
            public InputDeviceType deviceType;      // Device種別
            public SystemConfig.IdKey idKey;        // DeviceID, key

            public ListViewItem割り当て済み入力リスト用( InputDeviceType deviceType, SystemConfig.IdKey idKey )
            {
                this.割り当て可能 = true;
                this.deviceType = deviceType;
                this.idKey = idKey;

                switch( deviceType )
                {
                    case InputDeviceType.Keyboard:
                        this.Text = $"Keyboard, {idKey.key}, '{( (Keys) idKey.key ).ToString()}'";
                        break;

                    case InputDeviceType.GameController:
                        this.Text = $"GamePad, 0x{idKey.key:X8}, '{HID.GetUsageName( (uint) idKey.key )}'";
                        break;

                    case InputDeviceType.MidiIn:
                        this.Text = $"MidiIn[{idKey.deviceId}], Note:{idKey.key}";
                        break;

                    default:
                        throw new ArgumentException( "未対応のデバイスです。" );
                }
            }
        }

        /// <summary>
        ///     ダイアログで編集した内容は、このメンバにいったん保存される。
        /// </summary>
        private SystemConfig _変更後のシステム設定 = null!;

        private ドラム入力種別 _現在選択されているドラム入力種別 = ドラム入力種別.Unknown;

        private int _FootPedal現在値;

        /// <summary>
        ///     <see cref="_現在選択されているドラム入力種別"/> について、
        ///     <see cref="_変更後のシステム設定"/> の内容を割り当て済みリストに反映する。
        /// </summary>
        private void _割り当て済みリストを更新する( ListViewItem入力リスト用? 選択する項目 = null )
        {
            this.listView割り当て済み入力リスト.Items.Clear();


            // キーボードの反映

            var 現在選択されているドラム入力種別に割り当てられているキーボード入力
                = this._変更後のシステム設定.キーボードtoドラム.Where( ( kvp ) => ( kvp.Value == this._現在選択されているドラム入力種別 ) );

            foreach( var key in 現在選択されているドラム入力種別に割り当てられているキーボード入力 )
                this.listView割り当て済み入力リスト.Items.Add( new ListViewItem割り当て済み入力リスト用( InputDeviceType.Keyboard, key.Key ) );


            // ゲームコントローラの反映

            var 現在選択されているドラム入力種別に割り当てられているゲームコントローラ入力
                = this._変更後のシステム設定.ゲームコントローラtoドラム.Where( ( kvp ) => ( kvp.Value == this._現在選択されているドラム入力種別 ) );

            foreach( var key in 現在選択されているドラム入力種別に割り当てられているゲームコントローラ入力 )
                this.listView割り当て済み入力リスト.Items.Add( new ListViewItem割り当て済み入力リスト用( InputDeviceType.GameController, key.Key ) );


            // MIDI入力の反映

            var 現在選択されているドラム入力種別に割り当てられているMIDI入力 =
                this._変更後のシステム設定.MIDItoドラム.Where( ( kvp ) => ( kvp.Value == this._現在選択されているドラム入力種別 ) );

            foreach( var note in 現在選択されているドラム入力種別に割り当てられているMIDI入力 )
                this.listView割り当て済み入力リスト.Items.Add( new ListViewItem割り当て済み入力リスト用( InputDeviceType.MidiIn, note.Key ) );


            // 指定された項目があればフォーカスを変更する。

            if( null != 選択する項目 )
            {
                foreach( ListViewItem割り当て済み入力リスト用? item in this.listView割り当て済み入力リスト.Items )
                {
                    if( null != item &&
                        item.deviceType == 選択する項目.deviceType &&
                        item.idKey.deviceId == 選択する項目.inputEvent.DeviceID &&
                        item.idKey.key == 選択する項目.inputEvent.Key )
                    {
                        // MSDNより:
                        // https://msdn.microsoft.com/ja-jp/library/y4x56c0b(v=vs.110).aspx
                        // > 項目をプログラムで選択しても、フォーカスは自動的に ListView コントロールには変更されません。
                        // > そのため、項目を選択するときは、通常、その項目をフォーカスがある状態に設定します。
                        item.Focused = true;
                        item.Selected = true;
                    }
                }
            }
        }

        private void _現在選択されている入力リストの入力行を割り当て済み入力リストに追加する()
        {
            foreach( var itemobj in this.listView入力リスト.SelectedItems )
            {
                if( ( itemobj is ListViewItem入力リスト用 item ) &&   // 選択されているのが ListViewItem入力リスト用 じゃなければ何もしない。
                  ( item.割り当て可能 ) )                             // 割り当て可能のもののみ割り当てる。
                {
                    var idKey = new SystemConfig.IdKey( item.inputEvent );

                    switch( item.deviceType )
                    {
                        case InputDeviceType.Keyboard:
                            this._変更後のシステム設定.キーボードtoドラム[ idKey ] = this._現在選択されているドラム入力種別;   // 追加または更新
                            this._割り当て済みリストを更新する( item );
                            this.listView割り当て済み入力リスト.Focus();
                            this._変更あり = true;
                            break;

                        case InputDeviceType.GameController:
                            this._変更後のシステム設定.ゲームコントローラtoドラム[ idKey ] = this._現在選択されているドラム入力種別;   // 追加または更新
                            this._割り当て済みリストを更新する( item );
                            this.listView割り当て済み入力リスト.Focus();
                            this._変更あり = true;
                            break;

                        case InputDeviceType.MidiIn:
                            this._変更後のシステム設定.MIDItoドラム[ idKey ] = this._現在選択されているドラム入力種別;    // 追加または更新
                            this._割り当て済みリストを更新する( item );
                            this.listView割り当て済み入力リスト.Focus();
                            this._変更あり = true;
                            break;
                    }
                }
            }
        }


        private double _前回の入力リスト追加時刻;

        private void _一定時間が経っていれば空行を挿入する()
        {
            double 今回の入力リスト追加時刻 = QPCTimer.生カウント相対値を秒へ変換して返す( QPCTimer.生カウント );

            // 1秒以上経っていれば改行
            if( 1.0 < ( 今回の入力リスト追加時刻 - this._前回の入力リスト追加時刻 ) )
                this.listView入力リスト.Items.Add( "" );

            this._前回の入力リスト追加時刻 = 今回の入力リスト追加時刻;
        }


        private void comboBoxパッドリスト_SelectedIndexChanged( object sender, EventArgs e )
        {
            this._現在選択されているドラム入力種別 =
                (ドラム入力種別) Enum.Parse( typeof( ドラム入力種別 ), (string) this.comboBoxパッドリスト.SelectedItem );

            this._割り当て済みリストを更新する();
        }

        private void button追加_Click( object sender, EventArgs e )
        {
            this._現在選択されている入力リストの入力行を割り当て済み入力リストに追加する();
        }

        private void listView入力リスト_DoubleClick( object sender, EventArgs e )
        {
            this._現在選択されている入力リストの入力行を割り当て済み入力リストに追加する();
        }

        private void button割り当て解除_Click( object sender, EventArgs e )
        {
            // 選択されている項目に対応する入力をキーバインディングから削除する。

            foreach( ListViewItem割り当て済み入力リスト用? item in this.listView割り当て済み入力リスト.SelectedItems )
            {
                if( item is null )
                    continue;

                switch( item.deviceType )
                {
                    case InputDeviceType.Keyboard:
                        this._変更後のシステム設定.キーボードtoドラム.Remove( item.idKey );
                        this._変更あり = true;
                        break;

                    case InputDeviceType.GameController:
                        this._変更後のシステム設定.ゲームコントローラtoドラム.Remove( item.idKey );
                        this._変更あり = true;
                        break;

                    case InputDeviceType.MidiIn:
                        this._変更後のシステム設定.MIDItoドラム.Remove( item.idKey );
                        this._変更あり = true;
                        break;
                }
            }

            this._割り当て済みリストを更新する();
        }


        private bool _変更あり;

        private void 入力割り当てダイアログ_FormClosing( object sender, FormClosingEventArgs e )
        {
            // ※ウィンドウを閉じようとした時も Cancel になる。
            if( this.DialogResult == DialogResult.Cancel && this._変更あり )
            {
                var dr = MessageBox.Show( "変更を破棄していいですか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2 );

                if( dr == DialogResult.No )
                    e.Cancel = true;
            }
        }
    }
}
