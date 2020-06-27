#include "Texture.hlsli"

// ���_�V�F�[�_
//      ���̒��_�V�F�[�_�[�́A���_�f�[�^����؎󂯎�炸�A����ɒ��_�C���f�b�N�X�ivID = 0�`�j���󂯎��A
//      0�`3 �� vID �ɉ����āA���W�� (-0.5, -0.5)-(+0.5, +0.5) �ŌŒ肵����`�̒��_�f�[�^�𐶐�����B
//      ������A�萔�o�b�t�@�� World �s��Ŋg��k���E��]�E�ړ��ϊ�����B
//      �e���_�̃e�N�X�`�����W�iUV�j�́A�萔�o�b�t�@�Ŏw��ł���B
PS_INPUT main( uint vID : SV_VertexID )
{
	PS_INPUT vt;

	// ���_���W�i���f�����W�n�j�̐���
	switch( vID )
	{
	case 0:
		vt.Pos = float4( -0.5, 0.5, 0.0, 1.0 ); // ����
		vt.Tex = float2( TexLeft, TexTop );
		break;
	case 1:
		vt.Pos = float4( 0.5, 0.5, 0.0, 1.0 ); // �E��
		vt.Tex = float2( TexRight, TexTop );
		break;
	case 2:
		vt.Pos = float4( -0.5, -0.5, 0.0, 1.0 ); // ����
		vt.Tex = float2( TexLeft, TexBottom );
		break;
	default:
		vt.Pos = float4( 0.5, -0.5, 0.0, 1.0 ); // �E��
		vt.Tex = float2( TexRight, TexBottom );
		break;
	}

	// ���[���h�E�r���[�E�ˉe�ϊ�
	vt.Pos = mul( vt.Pos, World );
	vt.Pos = mul( vt.Pos, View );
	vt.Pos = mul( vt.Pos, Projection );

	// �o��
	return vt;
}
