using System;
using System.Collections.Generic;
using System.Diagnostics;
using SSTFormat.v004;

namespace DTXMania2.演奏
{
    /// <summary>
    ///     譜面のチップ（<see cref="チップ種別"/>）をキーとして、様々なコンフィグプロパティを定義する。
    /// </summary>
    class ドラムチッププロパティ
    {
        // 主キー

        public チップ種別 チップ種別 { get; set; }
        public レーン種別 レーン種別 { get; set; }

        // 表示

        public 表示レーン種別 表示レーン種別 { get; set; }
        public 表示チップ種別 表示チップ種別 { get; set; }

        // 入力

        public ドラム入力種別 ドラム入力種別 { get; set; }
        public AutoPlay種別 AutoPlay種別 { get; set; }
        public 入力グループ種別 入力グループ種別 { get; set; }

        // ヒット

        public bool 発声前消音 { get; set; }
        public 消音グループ種別 消音グループ種別 { get; set; }
        public bool AutoPlayON_自動ヒット => ( this.AutoPlayON_自動ヒット_再生 || this.AutoPlayON_自動ヒット_非表示 || this.AutoPlayON_自動ヒット_判定 );
        public bool AutoPlayON_自動ヒット_再生 { get; set; }
        public bool AutoPlayON_自動ヒット_非表示 { get; set; }
        public bool AutoPlayON_自動ヒット_判定 { get; set; }
        public bool AutoPlayON_Miss判定 { get; set; }
        public bool AutoPlayOFF_自動ヒット => ( this.AutoPlayOFF_自動ヒット_再生 || this.AutoPlayOFF_自動ヒット_非表示 || this.AutoPlayOFF_自動ヒット_判定 );
        public bool AutoPlayOFF_自動ヒット_再生 { get; set; }
        public bool AutoPlayOFF_自動ヒット_非表示 { get; set; }
        public bool AutoPlayOFF_自動ヒット_判定 { get; set; }
        public bool AutoPlayOFF_ユーザヒット => ( this.AutoPlayOFF_ユーザヒット_再生 || this.AutoPlayOFF_ユーザヒット_非表示 || this.AutoPlayOFF_ユーザヒット_判定 );
        public bool AutoPlayOFF_ユーザヒット_再生 { get; set; }
        public bool AutoPlayOFF_ユーザヒット_非表示 { get; set; }
        public bool AutoPlayOFF_ユーザヒット_判定 { get; set; }
        public bool AutoPlayOFF_Miss判定 { get; set; }
    }
}
