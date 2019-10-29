using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DTXMania2
{
    class ScreenMode
    {

        // プロパティ


        /// <summary>
        ///     現在ウィンドウモードであるならtrueを返す。
        /// </summary>
        public bool IsWindowMode { get; private set; } = true;

        /// <summary>
        ///     現在全画面モードであるならtrueを返す。
        /// </summary>
        public bool IsFullscreenMode
        {
            get => !IsWindowMode;
            set => IsWindowMode = !value;
        }



        // 生成と終了


        public ScreenMode( Form form )
        {
            this._Form = new WeakReference<Form>( form );
        }



        // 画面モードの切り替え


        /// <summary>
        ///     ウィンドウモードに切り替える。
        /// </summary>
        public void ToWindowMode()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( this._Form.TryGetTarget( out Form? form ) )
            {
                if( !( this.IsWindowMode ) )
                {
                    // UIスレッドで実行する。
                    form.BeginInvoke( new Action( () => {
                        using var _ = new LogBlock( "ウィンドウモードへの切り替え" );
                        this.IsWindowMode = true;
                        form.WindowState = FormWindowState.Normal;
                        form.ClientSize = this._ClientSize;
                        form.FormBorderStyle = this._formBorderStyle;
                        Cursor.Show();
                    } ) );
                }
                else
                {
                    Log.WARNING( $"すでにウィンドウモードなので、何もしません。" );
                }
            }
        }

        /// <summary>
        ///     全画面モードに切り替える。
        /// </summary>
        public void ToFullscreenMode()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( this._Form.TryGetTarget( out Form? form ) )
            {
                if( !( this.IsFullscreenMode ) )
                {
                    // UIスレッドで実行する。
                    form.BeginInvoke( new Action( () => {
                        using var _ = new LogBlock( "全画面モードへの切り替え" );
                        this.IsFullscreenMode = true;
                        // バックアップ
                        this._ClientSize = form.ClientSize;
                        this._formBorderStyle = form.FormBorderStyle;
                        // 正確には、「全画面(fullscreen)」ではなく「最大化(maximize)」。
                        // 参考: http://www.atmarkit.co.jp/ait/articles/0408/27/news105.html
                        form.WindowState = FormWindowState.Normal;
                        form.FormBorderStyle = FormBorderStyle.None;
                        form.WindowState = FormWindowState.Maximized;
                        Cursor.Hide();
                    } ) );
                }
                else
                {
                    Log.WARNING( $"すでに全画面モードなので、何もしません。" );
                }
            }
        }



        // ローカル


        private WeakReference<Form> _Form;

        private Size _ClientSize = new Size( 1024, 720 );

        private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
    }
}
