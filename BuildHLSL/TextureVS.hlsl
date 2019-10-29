#include "Texture.hlsli"

// 頂点シェーダ
//      この頂点シェーダーは、頂点データを一切受け取らず、代わりに頂点インデックス（vID = 0～）を受け取り、
//      0～3 の vID に応じて、座標を (-0.5, -0.5)-(+0.5, +0.5) で固定した矩形の頂点データを生成する。
//      これを、定数バッファの World 行列で拡大縮小・回転・移動変換する。
//      各頂点のテクスチャ座標（UV）は、定数バッファで指定できる。
PS_INPUT main( uint vID : SV_VertexID )
{
	PS_INPUT vt;

	// 頂点座標（モデル座標系）の生成
	switch( vID )
	{
	case 0:
		vt.Pos = float4( -0.5, 0.5, 0.0, 1.0 ); // 左上
		vt.Tex = float2( TexLeft, TexTop );
		break;
	case 1:
		vt.Pos = float4( 0.5, 0.5, 0.0, 1.0 ); // 右上
		vt.Tex = float2( TexRight, TexTop );
		break;
	case 2:
		vt.Pos = float4( -0.5, -0.5, 0.0, 1.0 ); // 左下
		vt.Tex = float2( TexLeft, TexBottom );
		break;
	default:
		vt.Pos = float4( 0.5, -0.5, 0.0, 1.0 ); // 右下
		vt.Tex = float2( TexRight, TexBottom );
		break;
	}

	// ワールド・ビュー・射影変換
	vt.Pos = mul( vt.Pos, World );
	vt.Pos = mul( vt.Pos, View );
	vt.Pos = mul( vt.Pos, Projection );

	// 出力
	return vt;
}
