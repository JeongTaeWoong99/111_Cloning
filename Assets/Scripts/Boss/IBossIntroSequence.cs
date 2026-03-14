using System.Collections;

/// <summary>
/// 보스 입장 연출을 정의하는 인터페이스.
/// 구현체가 없으면 BossManager가 즉시 전투를 시작한다 (NullObject 패턴).
/// </summary>
public interface IBossIntroSequence
{
    IEnumerator PlayIntro();
}
