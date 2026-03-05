def update(robot, dt):
    # Двигаемся вперёд, простая логика
    if (robot.CurrentSpeed < 10):
        robot.SetMotorPower(0.5, 0.5)
    else:
        robot.SetMotorPower(0, 0)
    print(f"Speed={robot.CurrentSpeed:.1f}km/h")