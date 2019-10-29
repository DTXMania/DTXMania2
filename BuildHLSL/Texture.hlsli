
// 定数バッファ
cbuffer cbCBuffer : register( b0 )
{
	matrix World;      // ワールド変換行列
	matrix View;       // ビュー変換行列
	matrix Projection; // 透視変換行列
	float TexLeft;     // 描画元矩形の左u座標
	float TexTop;      // 描画元矩形の上v座標
	float TexRight;    // 描画元矩形の右u座標
	float TexBottom;   // 描画元矩形の下v座標
	float TexAlpha;    // テクスチャ全体に乗じるアルファ値(0～1)
};

// ピクセルシェーダの入力データ
struct PS_INPUT
{
	float4 Pos : SV_POSITION; // 頂点座標(透視座標系)
	float2 Tex : TEXCOORD0;   // テクスチャ座標
};
