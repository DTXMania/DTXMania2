#include "Texture.hlsli"

Texture2D myTex2D : register( t0 );

SamplerState smpWrap : register( s0 );

// �s�N�Z���V�F�[�_
float4 main( PS_INPUT input ) : SV_TARGET
{
	// �e�N�X�`���擾
	float4 texCol = myTex2D.Sample( smpWrap, input.Tex ); // �e�N�Z���ǂݍ���
	texCol.a *= TexAlpha; // �A���t�@����Z

	// �F
	return saturate( texCol );
}
