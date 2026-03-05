import math

GX, GZ = 12.0, 61.0
GOAL_TOL = 2

V_MAX = 20.0
TURN_K = 1/20
MAX_T = 1

OBS_NEAR = 5.0          # раньше начинаем избегание
LOG_DT = 0.25

_t = 0.0
def _w(a): return (a + 180) % 360 - 180
def _c(v,a,b): return a if v<a else b if v>b else v

def update(robot, dt):
    global _t
    p = robot.Position
    x, z = float(p.x), float(p.z)
    yaw = float(robot.YawDeg)
    spd = float(robot.CurrentSpeed)

    dx, dz = GX - x, GZ - z
    if math.hypot(dx, dz) < GOAL_TOL:
        robot.Brake(1.0); robot.SetMotorPower(0, 0)
        _log(dt,"GOAL",x,z,yaw,spd,float('nan'),0.0,0.0,0); return

    nearest, n, lmin, rmin = 999.0, 0, 999.0, 999.0
    if robot.Lidars and len(robot.Lidars) > 0:
        L = robot.Lidars[0]
        nearest = float(L.Nearest)
        cloud = L.PointCloud or []
        n = len(cloud); h = n//2
        for i,pt in enumerate(cloud):
            d = float(pt.Distance)
            if i < h: lmin = min(lmin, d)
            else:     rmin = min(rmin, d)

    # базовый поворот к цели
    des = math.degrees(math.atan2(dx, dz))
    turn = _c(_w(des - yaw) * TURN_K, -MAX_T, MAX_T)
    thr, mode = 0.35, "DRIVE"

    # 1) лимит скорости: не просто thr=0, а ТОРМОЗ
    if spd >= V_MAX:
        robot.Brake(1.0)
        thr = 0.0
        mode = "LIMIT"

    # 2) препятствие: тормоз + поворот на месте в более свободную сторону
    if nearest < OBS_NEAR:
        robot.Brake(1.0)
        mode = "AVOID"
        t = MAX_T
        if lmin > rmin:  # слева дальше -> поворачиваем влево
            robot.SetMotorPower(-t, t)
            _log(dt,mode,x,z,yaw,spd,nearest,-t,t,n); return
        else:
            robot.SetMotorPower(t, -t)
            _log(dt,mode,x,z,yaw,spd,nearest,t,-t,n); return

    # обычная езда
    Lm, Rm = _c(thr - turn, -1, 1), _c(thr + turn, -1, 1)
    _log(dt,mode,x,z,yaw,spd,nearest,Lm,Rm,n)
    robot.SetMotorPower(Rm, Lm)

def _log(dt, mode, x, z, yaw, spd, near, L, R, n):
    global _t
    _t += dt
    if _t < LOG_DT: return
    _t = 0.0
    print(f"[{mode}] pos=({x:.1f},{z:.1f}) yaw={yaw:.0f} spd={spd:.1f} near={near:.1f} n={n} L={L:.2f} R={R:.2f}")