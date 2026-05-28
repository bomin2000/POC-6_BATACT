public enum WeaponState
{
    Spear,      // 휠 업: 장거리 직선 찌르기, 강한 단방향 넉백
    Boomerang,  // 휠 미들: 원거리 투척/귀환, 플레이어 방향 풀링
    Scissors,   // 휠 다운: 근거리 360도 회전 베기, 강한 경직
    BareHand    // 부메랑 투척 중 무기 없음, 변형 잠금 + 선입력 예약
}

public enum HitReactionType
{
    Knockback,
    PullToPlayer,
    StunLock
}
