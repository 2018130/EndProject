using UnityEngine;

public class WaterBalloonSkill : BaseSkill
{
    private LineRenderer trajectoryLine;

    public WaterBalloonSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player) { }


    public void StartHolding(PlayerNetwork player)
    {
        // 궤적 LineRenderer 생성
        GameObject lineObj = new GameObject("TrajectoryLine");
        trajectoryLine = lineObj.AddComponent<LineRenderer>();
        trajectoryLine.startWidth = 0.1f;
        trajectoryLine.endWidth = 0.1f;
        trajectoryLine.positionCount = 20;
    }
    public void StopHolding()
    {
        if (trajectoryLine != null)
            GameObject.Destroy(trajectoryLine.gameObject);
    }


    public void UpdateTrajectory(Vector3 startPos, Vector3 dir, float force)
    {
        // 포물선 계산
        Vector3 velocity = dir * force;
        for (int i = 0; i < 20; i++)
        {
            float t = i * 0.1f;
            Vector3 pos = startPos + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, pos);
        }
    }

    public void Throw(PlayerNetwork player, float throwAngle)
    {
        Vector3 throwDir = player.transform.forward + Vector3.up * throwAngle;
        throwDir.Normalize();
        StopHolding();
        player.UseSkillWithDir_ServerRpc(cardData.ID, throwDir);
    }
}