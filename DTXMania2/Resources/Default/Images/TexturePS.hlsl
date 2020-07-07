#include "Texture.hlsli"

Texture2D myTex2D : register( t0 );

SamplerState smpWrap : register( s0 );

// ピクセルシェーダ
float4 main( PS_INPUT input ) : SV_TARGET
{
	// テクスチャ取得
	float4 texCol = myTex2D.Sample( smpWrap, input.Tex ); // テクセル読み込み
	texCol.a *= TexAlpha; // アルファを乗算

	// 色
	return saturate( texCol );
}
