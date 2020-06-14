using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;
using FDK;
using System.Threading;

namespace DTXMania2.結果
{
    class ランク : IDisposable
    {

        // 生成と終了


        public ランク()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ランクエフェクト = new Dictionary<ランク種別, EffekseerNET.Effect>() {
                { ランク種別.SS, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankSS.efkefc" ).変数なしパス ) },
                { ランク種別.S, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankS.efkefc" ).変数なしパス ) },
                { ランク種別.A, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankA.efkefc" ).変数なしパス ) },
                { ランク種別.B, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankB.efkefc" ).変数なしパス ) },
                { ランク種別.C, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankC.efkefc" ).変数なしパス ) },
                { ランク種別.D, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankD.efkefc" ).変数なしパス ) },
                { ランク種別.E, EffekseerNET.Effect.Create( Global.Effekseer.Manager, new VariablePath( @"$(Images)\ResultStage\rankE.efkefc" ).変数なしパス ) },
            };

            // 0.6 秒後にエフェクト開始
            this._エフェクト開始イベント = new ManualResetEventSlim( false );
            this._エフェクト開始タイマ = new Timer( ( state ) => { this._エフェクト開始イベント.Set(); }, null, 600, Timeout.Infinite );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( 0 <= this._ランクエフェクトハンドル )
            {
                Global.Effekseer.Manager.StopEffect( this._ランクエフェクトハンドル );
                Global.Effekseer.Manager.SetShown( this._ランクエフェクトハンドル, false );
            }

            this._エフェクト開始タイマ.Dispose();
            this._エフェクト開始イベント.Dispose();

            //foreach( var rankEffect in this._ランクエフェクト.Values )
            //    rankEffect.Dispose(); --> 不要
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, ランク種別 rank )
        {
            this._Rank = rank;

            if( 0 > this._ランクエフェクトハンドル &&
                this._エフェクト開始イベント.IsSet )
            {
                this._ランクエフェクトを開始する();
                this._エフェクト開始イベント.Reset();
            }

            if( 0 <= this._ランクエフェクトハンドル )
            {
                dc.EndDraw();   // D2D中断
                Global.Effekseer.描画する( this._ランクエフェクトハンドル );
                dc.BeginDraw(); // D2D再開
            }
        }

        public void アニメを完了する()
        {
            if( 0 > this._ランクエフェクトハンドル )
                this._ランクエフェクトを開始する();
        }

        private void _ランクエフェクトを開始する()
        {
            this._ランクエフェクトハンドル = Global.Effekseer.Manager.Play( this._ランクエフェクト[ this._Rank ], -4.5f, 0.4f, -0f );
            Global.Effekseer.Manager.SetRotation( this._ランクエフェクトハンドル, new EffekseerNET.Vector3D( 1, 0, 0 ), 0.2f );   // 少し前傾
        }



        // ローカル


        private readonly Dictionary<ランク種別, EffekseerNET.Effect> _ランクエフェクト;

        private int _ランクエフェクトハンドル = -1;

        private ManualResetEventSlim _エフェクト開始イベント;

        private Timer _エフェクト開始タイマ;

        private ランク種別 _Rank;
    }
}
