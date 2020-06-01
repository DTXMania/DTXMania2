using System;
using System.Collections.Generic;
using System.Text;
using SharpDX.Animation;

namespace FDK
{
    public class TraceValue : IDisposable
    {

        // プロパティ


        public double 目標値
        {
            get => this._目標値;
            set
            {
                this._目標値 = value;
                this._追従値アニメを再構築する();
            }
        }

        public double 現在値
            => this._現在値.Value;


        public double 切替時間sec { get; }

        public double 速度係数 { get; }


        // 生成と終了


        public TraceValue( Animation animation, double 初期値, double 切替時間sec, double 速度係数 = 1.0 )
        {
            this._Animation = animation;

            this._目標値 = 初期値;
            this._現在値 = new Variable( this._Animation.Manager, (double) 初期値 );
            this.切替時間sec = 切替時間sec;
            this.速度係数 = 速度係数;

            this._ストーリーボード = null!;
        }

        public void Dispose()
        {
            this._ストーリーボード?.Dispose();
            this._現在値?.Dispose();
        }



        // ローカル


        private Animation _Animation;

        private double _目標値;

        private Variable _現在値;

        private Storyboard _ストーリーボード;


        private void _追従値アニメを再構築する()
        {
            this._ストーリーボード?.Dispose();
            this._ストーリーボード = new Storyboard( this._Animation.Manager );

            using( var 遷移 = this._Animation.TrasitionLibrary.Linear( this.切替時間sec, finalValue: (double) this._目標値 ) )
                this._ストーリーボード.AddTransition( this._現在値, 遷移 );

            this._ストーリーボード.Schedule( this._Animation.Timer.Time );
        }
    }
}
