
// �萔�o�b�t�@
cbuffer cbCBuffer : register( b0 )
{
	matrix World;      // ���[���h�ϊ��s��
	matrix View;       // �r���[�ϊ��s��
	matrix Projection; // �����ϊ��s��
	float TexLeft;     // �`�挳��`�̍�u���W
	float TexTop;      // �`�挳��`�̏�v���W
	float TexRight;    // �`�挳��`�̉Eu���W
	float TexBottom;   // �`�挳��`�̉�v���W
	float TexAlpha;    // �e�N�X�`���S�̂ɏ悶��A���t�@�l(0�`1)
};

// �s�N�Z���V�F�[�_�̓��̓f�[�^
struct PS_INPUT
{
	float4 Pos : SV_POSITION; // ���_���W(�������W�n)
	float2 Tex : TEXCOORD0;   // �e�N�X�`�����W
};
