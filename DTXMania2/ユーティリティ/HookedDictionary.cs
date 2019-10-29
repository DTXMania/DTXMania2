using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///		インデクサでのset/getアクションを指定できるDictionary。
    /// </summary>
    class HookedDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
    {
        public Action<TKey, TValue>? get時アクション = null;

        public Action<TKey, TValue>? set時アクション = null;

        public new TValue this[ TKey key ]
        {
            get
            {
                if( !this.TryGetValue( key, out TValue value ) )
                    throw new KeyNotFoundException();

                this.get時アクション?.Invoke( key, value );   // Hook
                return value;
            }
            set
            {
                if( this.ContainsKey( key ) )
                    this.Remove( key );
                this.Add( key, value );

                this.set時アクション?.Invoke( key, value );   // Hook
            }
        }
    }
}
