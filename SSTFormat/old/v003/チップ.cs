using System;
using System.Collections.Generic;

namespace SSTFormat.v003
{
    public class チップ : IComparable, ICloneable
    {

        // 定数


        public const int 最大音量 = 8;
        public const int 既定音量 = 7;



        // チップの種類


        public チップ種別 チップ種別 { get; set; } = チップ種別.Unknown;



        // チップの配置


        /// <summary>
        ///     このチップが存在する小節の番号。-1 以上の整数。
        ///     小節番号 -1 の小節内には、発声可能なチップを配置しないこと。（小節線チップなどは可）
        /// </summary>
        public int 小節番号 { get; set; } = -1;

        /// <summary>
        ///     このチップが存在する小節の解像度。
        ///     チップは 0～小節解像度-1 の範囲に配置できる。
        /// </summary>
        /// <remarks>
        ///     <see cref="小節内位置"/> と <see cref="小節解像度"/> の組でこのチップの位置を表す。
        ///     この組はチップごとに持つので、小節内のすべてのチップの解像度が等しいという必要はない。
        /// </remarks>
        public int 小節解像度 { get; set; } = 1;

        /// <summary>
        ///     このチップの位置。
        ///     チップの位置は、小節内の相対位置「小節内位置 ÷ 小節解像度」（＝ 0:小節先頭 ～ 1:小節末尾）で表される。
        /// </summary>
        public int 小節内位置 { get; set; } = 0;



        // チップの描画/発声時刻


        /// <summary>
        ///		チップの描画時刻を、譜面の先頭（小節番号 -1 の小節の先頭）からの相対時間（秒単位）で表す。
        ///		未使用（描画しないタイプのチップ）なら負数。
        /// </summary>
        public double 描画時刻sec { get; set; } = -1;

        /// <summary>
        ///		チップの発声時刻[sec]。
        ///		譜面の先頭（小節番号 -1 の小節の先頭）からの時刻を秒単位で表す。
        ///		未使用（発声しないタイプのチップ）なら負数。
        /// </summary>
        /// <remarks>
        ///		サウンドの発声遅延を考慮して、描画時刻よりも遅く設定すること。
        /// </remarks>
        public double 発声時刻sec { get; set; } = -1;



        // チップのその他のプロパティ


        /// <summary>
        ///		チップの種別をより詳細に示すためのユーザ定義ID。
        /// </summary>
        /// <remarks>
        ///		今のところ、DTXから変換した場合に、ここにオブジェクト値が格納される。
        /// </remarks>
        public int チップサブID { get; set; } = 0;

        /// <summary>
        ///		チップの音量（小:1～<see cref="最大音量"/>:大）。
        ///		最小値は 0 じゃなく 1 なので注意。
        /// </summary>
        public int 音量
        {
            get
                => this._音量;
            set
                => this._音量 = ( ( 1 > value ) || ( チップ.最大音量 < value ) ) ?
                    throw new ArgumentException( $"音量の値域(1～{チップ.最大音量})を超える値 '{value}' が指定されました。" ) :
                    value;
        }

        /// <summary>
        ///		左右の発声位置。PAN。
        ///		左:-100 ～ 中央:0 ～ +100:右。
        /// </summary>
        public int 左右位置
        {
            get
                => this._位置;
            set
                => this._位置 = ( -100 > value || +100 < value ) ?
                    throw new ArgumentOutOfRangeException( $"位置の値域(-100～+100)を超える値 '{value}' が指定されました。" ) :
                    value;
        }

        /// <summary>
        ///		<see cref="チップ種別"/> が <see cref="チップ種別.BPM"/> である場合は、その BPM 値。
        ///		それ以外の場合は無効。
        /// </summary>
        public double BPM { get; set; } = 120.0;

        /// <summary>
        ///     true であればチップを表示してもよいが、false であれば表示してはならない。
        ///     ただし、チップが表示不可能なもの（小節の先頭など）である場合は、true/false のいずれであっても表示しなくていい。
        /// </summary>
        public bool 可視 { get; set; } = true;



        // メソッド


        public チップ()
        {
        }

        public チップ( v002.チップ v2chip )
        {
            this.チップ種別 = (チップ種別) v2chip.チップ種別;
            this.チップサブID = 0;   // v3で新規追加
            this.小節番号 = v2chip.小節番号;
            this.小節内位置 = v2chip.小節内位置;
            this.小節解像度 = v2chip.小節解像度;
            this.描画時刻sec = v2chip.描画時刻ms / 1000.0;
            this.発声時刻sec = v2chip.発声時刻ms / 1000.0;
            this.音量 = v2chip.音量;
            this.BPM = v2chip.BPM;
            this.可視 = true; // v3で新規追加
        }

        /// <summary>
        ///     チップの深いコピーを生成して返す。
        /// </summary>
        public virtual object Clone()
        {
            // 浅いコピー
            var dst = this.MemberwiseClone();

            // 参照型フィールドのコピー：必要ならここに記述すること。

            return dst;
        }

        /// <summary>
        ///     チップの内容を自身にコピーする。
        /// </summary>
        public void CopyFrom( チップ src )
        {
            this.チップ種別 = src.チップ種別;

            this.小節番号 = src.小節番号;
            this.小節解像度 = src.小節解像度;
            this.小節内位置 = src.小節内位置;

            this.描画時刻sec = src.描画時刻sec;
            this.発声時刻sec = src.発声時刻sec;

            this.チップサブID = src.チップサブID;
            this.音量 = src.音量;
            this.左右位置 = src.左右位置;
            this.BPM = src.BPM;
            this.可視 = src.可視;
        }

        // 概要:
        //     現在のインスタンスを同じ型の別のオブジェクトと比較して、並べ替え順序において、現在のインスタンスの位置が同じ型の別のオブジェクトの前、後ろ、または同じのいずれであるかを示す整数を返します。
        //
        // パラメータ:
        //   obj:
        //     このインスタンスと比較するオブジェクト。
        //
        // 戻り値:
        //     比較対象オブジェクトの相対順序を示す 32 ビット符号付き整数。戻り値の意味は次のとおりです。 
        //
        //     値		説明
        //     --------------------
        //     負数		this ＜ obj
        //     0		this ＝ obj
        //     正数		this ＞ obj
        //
        // 例外:
        //   System.ArgumentException:
        //     obj の型がこのインスタンスの型と異なります。
        //
        public int CompareTo( object obj )
        {
            var other = obj as チップ;

            if( this.小節番号 < other.小節番号 ) { return -1; }
            if( this.小節番号 > other.小節番号 ) { return +1; }

            double dbThis = (double) this.小節内位置 / (double) this.小節解像度;
            double dbOther = (double) other.小節内位置 / (double) other.小節解像度;

            if( dbThis < dbOther ) { return -1; }
            if( dbThis > dbOther ) { return +1; }


            // グリッドが完全に等しいなら、チップの種類ごとに定義された深度で順序を決める。

            if( チップ.チップの深さ[ this.チップ種別 ] > チップ.チップの深さ[ other.チップ種別 ] ) { return -1; }
            if( チップ.チップの深さ[ this.チップ種別 ] < チップ.チップの深さ[ other.チップ種別 ] ) { return +1; }

            return 0;
        }



        // ローカル


        protected readonly static Dictionary<チップ種別, int> チップの深さ = new Dictionary<チップ種別, int>() {
            #region " *** "
            //-----------------
            { チップ種別.Ride_Cup, 50 },
            { チップ種別.HiHat_Open, 50 },
            { チップ種別.HiHat_HalfOpen, 50 },
            { チップ種別.HiHat_Close, 50 },
            { チップ種別.HiHat_Foot, 50 },
            { チップ種別.Snare, 50 },
            { チップ種別.Snare_OpenRim, 50 },
            { チップ種別.Snare_ClosedRim, 50 },
            { チップ種別.Snare_Ghost, 50 },
            { チップ種別.Tom1, 50 },
            { チップ種別.Tom1_Rim, 50 },
            { チップ種別.BPM, 50 },
            { チップ種別.Ride, 60 },
            { チップ種別.Splash, 60 },
            { チップ種別.Tom2, 60 },
            { チップ種別.Tom2_Rim, 60 },
            { チップ種別.LeftCrash, 70 },
            { チップ種別.China, 70 },
            { チップ種別.Tom3, 70 },
            { チップ種別.Tom3_Rim, 70 },
            { チップ種別.RightCrash, 70 },
            { チップ種別.Bass, 74 },
            { チップ種別.LeftBass, 75 },
            { チップ種別.LeftCymbal_Mute, 76 },
            { チップ種別.RightCymbal_Mute, 76 },
            { チップ種別.小節線, 80 },
            { チップ種別.拍線, 85 },
            { チップ種別.背景動画, 90 },
            { チップ種別.小節メモ, 99 },
            { チップ種別.小節の先頭, 99 },
            { チップ種別.BGM, 99 },
            { チップ種別.SE1, 99 },
            { チップ種別.SE2, 99 },
            { チップ種別.SE3, 99 },
            { チップ種別.SE4, 99 },
            { チップ種別.SE5, 99 },
            { チップ種別.GuitarAuto, 99 },
            { チップ種別.BassAuto, 99 },
            { チップ種別.Unknown, 99 },
            //-----------------
            #endregion
        };

        private int _音量 = チップ.最大音量;

        private int _位置 = 0;
    }
}
