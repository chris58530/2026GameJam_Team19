using UnityEngine;

/// <summary>
/// 2D 相機跟隨控制器:平滑跟隨目標,且「離越遠跟越快」。
///
/// 原理:
/// - 速度正比於與目標的距離(指數平滑),這樣靠近時慢、遠離時快,自然流暢。
/// - 再加一個 distancePower 指數,讓遠的時候更非線性地加速(例如 1.5 = 遠距離追擊感)。
/// - 可選 deadZone:在小框內目標移動相機不動,框外才開始跟。
/// - 用 LateUpdate 確保在角色當幀移動後再更新相機,避免抖動。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraFollow2D : MonoBehaviour
{
    [Header("跟隨目標")]
    [Tooltip("要跟隨的目標 (通常拖入主角 Transform)。若留空且場景中有 Tag=Player 的物件會自動抓取。")]
    public Transform target;

    [Tooltip("相機相對目標的位置偏移 (世界座標)。X/Y 用來偏移畫面,Z 通常維持 -10。")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("跟隨手感")]
    [Tooltip("基礎跟隨速度 (1/秒)。等同指數平滑的 lambda,值越大越緊跟。約 2~8 為佳。")]
    [Min(0f)]
    public float followSharpness = 4f;

    [Tooltip("Y 軸獨立跟隨速度 (1/秒)。設較小的值可讓垂直方向跟得慢一點 (例如跳躍時不會馬上拉上去)。" +
             "設 0 表示沿用 followSharpness。")]
    [Min(0f)]
    public float followSharpnessY = 1.5f;

    [Tooltip("距離放大指數。1 = 線性(純指數平滑);> 1 = 距離越遠越爆發(例如 1.5、2)。")]
    [Min(1f)]
    public float distancePower = 1.5f;

    [Tooltip("最大跟隨速度 (單位/秒)。0 表示不限制。用來避免瞬移時的爆衝。")]
    [Min(0f)]
    public float maxFollowSpeed = 0f;

    [Header("死區 (可選)")]
    [Tooltip("目標在這個矩形內時,相機完全不動 (X/Y 為半寬、半高)。設 0 = 不使用死區。")]
    public Vector2 deadZone = Vector2.zero;

    [Header("Z 軸")]
    [Tooltip("是否鎖定 Z 為 offset.z (建議 2D 開啟,避免相機被拉到不對的深度)。")]
    public bool lockZ = true;

    private void Reset()
    {
        // 新增元件時給一個合理預設:Z = 當前相機 Z,通常是 -10
        offset = new Vector3(0f, 1f, transform.position.z);
    }

    private void Start()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }

        // 啟動時先對齊一次,避免從原點瞬移過去
        if (target != null)
        {
            transform.position = ComposeTargetPos();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = ComposeTargetPos();
        Vector3 current = transform.position;
        if (lockZ) current.z = desired.z;

        Vector3 delta = desired - current;

        // 死區:目標在死區內就不動
        Vector2 ax = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        if (ax.x <= deadZone.x) delta.x = 0f;
        else delta.x -= Mathf.Sign(delta.x) * deadZone.x;
        if (ax.y <= deadZone.y) delta.y = 0f;
        else delta.y -= Mathf.Sign(delta.y) * deadZone.y;

        // 「離越遠跟越快」:位移量做指數放大,再以 followSharpness 做指數平滑
        // 公式:step = delta * (1 - exp(-followSharpness * dt)) * distance^(distancePower - 1)
        // 等價於速度 v = k * |delta|^distancePower 的時間離散化,且天生 frame-rate independent。
        // X 與 Y 分開計算,讓 Y 軸可以跟得比 X 慢 (跳躍時不會立刻把畫面拉上去)。
        float dt = Time.deltaTime;
        float distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            float boost = (distancePower > 1f) ? Mathf.Pow(distance, distancePower - 1f) : 1f;
            float sharpY = (followSharpnessY > 0f) ? followSharpnessY : followSharpness;

            float tx = 1f - Mathf.Exp(-followSharpness * boost * dt);
            float ty = 1f - Mathf.Exp(-sharpY * boost * dt);

            Vector3 step = new Vector3(delta.x * tx, delta.y * ty, delta.z * tx);

            // 速度上限 (可選)
            if (maxFollowSpeed > 0f)
            {
                float maxStep = maxFollowSpeed * dt;
                if (step.magnitude > maxStep)
                    step = step.normalized * maxStep;
            }

            current += step;
        }

        if (lockZ) current.z = desired.z;
        transform.position = current;
    }

    private Vector3 ComposeTargetPos()
    {
        Vector3 p = target.position + offset;
        if (lockZ) p.z = offset.z;
        return p;
    }

    private void OnDrawGizmosSelected()
    {
        if (deadZone.sqrMagnitude <= 0f) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Vector3 c = (target != null) ? target.position + new Vector3(offset.x, offset.y, 0f) : transform.position;
        c.z = transform.position.z;
        Gizmos.DrawWireCube(c, new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0f));
    }
}
