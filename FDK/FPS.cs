using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FDK
{
    /// <summary>
    ///		FPS（１秒間の進行処理回数）と VPS（１秒間の描画処理回数）を計測する。
    /// </summary>
    /// <remarks>
    ///		FPSをカウントする() を呼び出さないと、VPS も更新されないので注意。
    /// </remarks>
    public class FPS
    {

        // プロパティ


        public int 現在のFPS
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return this._現在のFPS;
                }
            }
        }

        public int 現在のVPS
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return this._現在のVPS;
                }
            }
        }



        // 生成と終了


        public FPS()
        {
            this._現在のFPS = 0;
            this._現在のVPS = 0;

            this._初めてのFPS更新 = true;
        }



        // カウント


        /// <summary>
        ///		FPSをカウントUPして、<see cref="現在のFPS"/>, <see cref="現在のVPS"/> プロパティに現在の値をセットする。
        ///		VPSはカウントUPしない。
        /// </summary>
        /// <returns>現在のFPS/VPSを更新した場合は true。</returns>
        public bool FPSをカウントしプロパティを更新する()
        {
            lock( this._スレッド間同期 )
            {
                if( this._初めてのFPS更新 )
                {
                    this._初めてのFPS更新 = false;
                    this._fps用カウンタ = 0;
                    this._vps用カウンタ = 0;
                    this._定間隔進行 = new 定間隔進行();  // 計測開始
                    return false;
                }
                else
                {
                    // FPS 更新。
                    this._fps用カウンタ++;

                    // 1秒ごとに FPS, VPS プロパティの値を更新。
                    this._定間隔進行.経過時間の分だけ進行する( 1000, () => {
                        this._現在のFPS = this._fps用カウンタ;
                        this._現在のVPS = this._vps用カウンタ;
                        this._fps用カウンタ = 0;
                        this._vps用カウンタ = 0;
                    } );

                    return ( 0 == this._fps用カウンタ );
                }
            }
        }

        /// <summary>
        ///		VPSをカウントUPする。FPSはカウントUPしない。
        ///		<see cref="現在のFPS"/>, <see cref="現在のVPS"/> プロパティは更新しない。
        /// </summary>
        public void VPSをカウントする()
        {
            lock( this._スレッド間同期 )
            {
                // VPS 更新。
                this._vps用カウンタ++;
            }
        }



        // ローカル


        private int _現在のFPS;

        private int _現在のVPS;

        private int _fps用カウンタ = 0;

        private int _vps用カウンタ = 0;

        private bool _初めてのFPS更新;

        private 定間隔進行 _定間隔進行 = null!;

        private readonly object _スレッド間同期 = new object();
    }
}
