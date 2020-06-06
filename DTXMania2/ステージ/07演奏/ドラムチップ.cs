using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX;
using FDK;
using SSTFormat.v004;

namespace DTXMania2.演奏
{
    class ドラムチップ : IDisposable
    {

        // 生成と終了


        public ドラムチップ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ドラムチップ画像 = new 画像( @"$(Images)\PlayStage\DrumChip.png" );
            this._ドラムチップの矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\DrumChip.yaml" );
            this._ドラムチップアニメ = new LoopCounter( 0, 48, 10 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ドラムチップ画像.Dispose();
        }



        // 進行と描画


        /// <returns>クリアしたらtrueを返す。</returns>
        public bool 進行描画する( ref int 描画開始チップ番号, チップの演奏状態 state, チップ chip, int index, double ヒット判定バーとの距離dpx )
        {
            float たて中央位置dpx = (float) ( 演奏ステージ.ヒット判定位置Ydpx + ヒット判定バーとの距離dpx );
            float 消滅割合 = 0f;

            #region " 消滅割合を算出; チップがヒット判定バーを通過したら徐々に消滅する。"
            //----------------
            const float 消滅を開始するヒット判定バーからの距離dpx = 20f;
            const float 消滅開始から完全消滅するまでの距離dpx = 70f;

            if( 消滅を開始するヒット判定バーからの距離dpx < ヒット判定バーとの距離dpx )   // 通過した
            {
                // 通過距離に応じて 0→1の消滅割合を付与する。0で完全表示、1で完全消滅、通過してなければ 0。
                消滅割合 = Math.Min( 1f, (float) ( ( ヒット判定バーとの距離dpx - 消滅を開始するヒット判定バーからの距離dpx ) / 消滅開始から完全消滅するまでの距離dpx ) );
            }
            //----------------
            #endregion

            #region " チップが描画開始チップであり、かつ、そのY座標が画面下端を超えたなら、描画開始チップ番号を更新する。"
            //----------------
            if( ( index == 描画開始チップ番号 ) &&
                ( Global.設計画面サイズ.Height + 40.0 < たて中央位置dpx ) )   // +40 はチップが隠れるであろう適当なマージン。
            {
                描画開始チップ番号++;

                // 描画開始チップがチップリストの末尾に到達したら、演奏を終了する。
                if( Global.App.演奏スコア.チップリスト.Count <= 描画開始チップ番号 )
                {
                    描画開始チップ番号 = -1;    // 演奏完了。
                    return true;                // クリア。
                }

                return false;
            }
            //----------------
            #endregion

            if( state.不可視 )
                return false;

            var 大きさ0to1 = new Size2F( 1f, 1f ); // 音量を反映した大きさ（縦横の倍率）。
            var 等倍 = new Size2F( 1f, 1f );       // 音量を反映しない場合はこっちを使う。
            
            var userConfig = Global.App.ログオン中のユーザ;

            #region " 音量からチップの大きさを計算する。"
            //----------------
            if( userConfig.音量に応じてチップサイズを変更する )
            {
                if( chip.チップ種別 != チップ種別.Snare_Ghost )   // Ghost は対象外
                {
                    // 既定音量未満は大きさを小さくするが、既定音量以上は大きさ1.0のままとする。最小は 0.3。
                    大きさ0to1 = new Size2F( 1f, MathUtil.Clamp( chip.音量 / (float) チップ.既定音量, 0.3f, 1.0f ) );   // 現状、音量は縦方向にのみ影響する。
                }
            }
            //----------------
            #endregion

            // チップ種別 から、表示レーン種別 と 表示チップ種別 を取得。
            var 表示レーン種別 = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ].表示レーン種別;
            var 表示チップ種別 = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ].表示チップ種別;

            if( ( 表示レーン種別 != 表示レーン種別.Unknown ) &&   // Unknwon ならチップを表示しない。
                ( 表示チップ種別 != 表示チップ種別.Unknown ) )    //
            {
                #region " チップを描画する。"
                //----------------
                switch( chip.チップ種別 )
                {
                    case チップ種別.LeftCrash:
                        this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftCymbal.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.HiHat_Close:
                        this._アニメチップを１つ描画する( 表示レーン種別.HiHat, this._ドラムチップの矩形リスト[ 表示チップ種別.HiHat.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.HiHat_HalfOpen:
                        this._アニメチップを１つ描画する( 表示レーン種別.HiHat, this._ドラムチップの矩形リスト[ 表示チップ種別.HiHat.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        this._単画チップを１つ描画する( 表示レーン種別.Foot, this._ドラムチップの矩形リスト[ 表示チップ種別.HiHat_HalfOpen.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.HiHat_Open:
                        this._アニメチップを１つ描画する( 表示レーン種別.HiHat, this._ドラムチップの矩形リスト[ 表示チップ種別.HiHat.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        this._単画チップを１つ描画する( 表示レーン種別.Foot, this._ドラムチップの矩形リスト[ 表示チップ種別.HiHat_Open.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.HiHat_Foot:
                        this._単画チップを１つ描画する( 表示レーン種別.Foot, this._ドラムチップの矩形リスト[ 表示チップ種別.Foot.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.Snare:
                        this._アニメチップを１つ描画する( 表示レーン種別.Snare, this._ドラムチップの矩形リスト[ 表示チップ種別.Snare.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Snare_ClosedRim:
                        this._単画チップを１つ描画する( 表示レーン種別.Snare, this._ドラムチップの矩形リスト[ 表示チップ種別.Snare_ClosedRim.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.Snare_OpenRim:
                        this._単画チップを１つ描画する( 表示レーン種別.Snare, this._ドラムチップの矩形リスト[ 表示チップ種別.Snare_OpenRim.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        //this._単画チップを１つ描画する( 表示レーン種別.Snare, this._ドラムチップの矩形リスト[ 表示チップ種別.Snare.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        // → ないほうがいいかも。
                        break;

                    case チップ種別.Snare_Ghost:
                        this._単画チップを１つ描画する( 表示レーン種別.Snare, this._ドラムチップの矩形リスト[ 表示チップ種別.Snare_Ghost.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.Bass:
                        this._アニメチップを１つ描画する( 表示レーン種別.Bass, this._ドラムチップの矩形リスト[ 表示チップ種別.Bass.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.LeftBass:
                        this._アニメチップを１つ描画する( 表示レーン種別.Bass, this._ドラムチップの矩形リスト[ 表示チップ種別.Bass.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Tom1:
                        this._アニメチップを１つ描画する( 表示レーン種別.Tom1, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom1.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Tom1_Rim:
                        this._単画チップを１つ描画する( 表示レーン種別.Tom1, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom1_Rim.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.Tom2:
                        this._アニメチップを１つ描画する( 表示レーン種別.Tom2, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom2.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Tom2_Rim:
                        this._単画チップを１つ描画する( 表示レーン種別.Tom2, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom2_Rim.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.Tom3:
                        this._アニメチップを１つ描画する( 表示レーン種別.Tom3, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom3.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Tom3_Rim:
                        this._単画チップを１つ描画する( 表示レーン種別.Tom3, this._ドラムチップの矩形リスト[ 表示チップ種別.Tom3_Rim.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.RightCrash:
                        this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightCymbal.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.China:
                        if( userConfig.表示レーンの左右.Chinaは左 )
                            this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftChina.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        else
                            this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightChina.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Ride:
                        if( userConfig.表示レーンの左右.Rideは左 )
                            this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftRide.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        else
                            this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightRide.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Ride_Cup:
                        if( userConfig.表示レーンの左右.Rideは左 )
                            this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftRide_Cup.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        else
                            this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightRide_Cup.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.Splash:
                        if( userConfig.表示レーンの左右.Splashは左 )
                            this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftSplash.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        else
                            this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightSplash.ToString() ]!.Value, たて中央位置dpx, 大きさ0to1, 消滅割合 );
                        break;

                    case チップ種別.LeftCymbal_Mute:
                        this._単画チップを１つ描画する( 表示レーン種別.LeftCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.LeftCymbal_Mute.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;

                    case チップ種別.RightCymbal_Mute:
                        this._単画チップを１つ描画する( 表示レーン種別.RightCymbal, this._ドラムチップの矩形リスト[ 表示チップ種別.RightCymbal_Mute.ToString() ]!.Value, たて中央位置dpx, 等倍, 消滅割合 );
                        break;
                }
                //----------------
                #endregion
            }

            return false;
        }



        // ローカル


        private readonly 画像 _ドラムチップ画像;

        private readonly 矩形リスト _ドラムチップの矩形リスト;

        private readonly LoopCounter _ドラムチップアニメ;

        private const float _チップの最終調整倍率 = 1.2f; // 見た感じで決めた主観的な値。

        private void _単画チップを１つ描画する( 表示レーン種別 lane, RectangleF 転送元矩形, float 上位置, Size2F 大きさ0to1, float 消滅割合 )
        {
            float X倍率 = 大きさ0to1.Width;
            float Y倍率 = 大きさ0to1.Height;

            if( lane == 表示レーン種別.LeftCymbal || lane == 表示レーン種別.RightCymbal )
            {
                // シンバルレーンは大きさの変化をより少なく、さらにX倍率もY倍率と同じにする。
                X倍率 = MathUtil.Clamp( 大きさ0to1.Width * 2f, min: 0f, max: 1f );
                Y倍率 = MathUtil.Clamp( 大きさ0to1.Height * 2f, min: 0f, max: 1f );
            }

            X倍率 *= ( 1f - 消滅割合 ) * _チップの最終調整倍率;
            Y倍率 *= ( 1f - 消滅割合 ) * _チップの最終調整倍率;

            this._ドラムチップ画像.進行描画する(
                左位置: レーンフレーム.レーン中央位置X[ lane ] - ( 転送元矩形.Width * X倍率 / 2f ),
                上位置: 上位置 - ( 転送元矩形.Height * Y倍率 / 2f ),
                転送元矩形: 転送元矩形,
                X方向拡大率: X倍率,
                Y方向拡大率: Y倍率 );
        }

        private void _アニメチップを１つ描画する( 表示レーン種別 lane, RectangleF 転送元矩形, float Y, Size2F 大きさ0to1, float 消滅割合 )
        {
            float X倍率 = 大きさ0to1.Width;
            float Y倍率 = 大きさ0to1.Height;

            if( lane == 表示レーン種別.Bass )
            {
                Y倍率 *= 1.2f;    // Bass は縦方向に少し大きめに。
            }

            X倍率 *= ( 1f - 消滅割合 ) * _チップの最終調整倍率;
            Y倍率 *= ( 1f - 消滅割合 ) * _チップの最終調整倍率;

            const float チップ1枚の高さ = 18f;

            転送元矩形.Offset( 0f, this._ドラムチップアニメ.現在値 * 15f );   // 下端3pxは下のチップと共有する前提のデザインなので、18f-3f = 15f。
            転送元矩形.Height = チップ1枚の高さ;

            this._ドラムチップ画像.進行描画する(
                左位置: レーンフレーム.レーン中央位置X[ lane ] - ( 転送元矩形.Width * X倍率 / 2f ),
                上位置: Y - ( チップ1枚の高さ * Y倍率 / 2f ),
                転送元矩形: 転送元矩形,
                X方向拡大率: X倍率,
                Y方向拡大率: Y倍率 );
        }
    }
}
