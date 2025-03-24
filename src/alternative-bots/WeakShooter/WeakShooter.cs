using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class WeakShooter : Bot
{
    static void Main(string[] args)
    {
        new WeakShooter().Start();
    }

    WeakChaser() : base(BotInfo.FromFile("WeakShooter.json")) { }

    private EnemyBot targetBot = null;

    public override void Run()
    {
        while (IsRunning)
        {
            AdjustRadarForBodyTurn = true;
            AdjustRadarForGunTurn = true;
            AdjustGunForBodyTurn = true;
            RadarTurnRate = MaxRadarTurnRate * 9 / 10;
            SetTurnRadarRight(360); 
            

            if (targetBot != null)
            {
                AimAndFire(targetBot);
            }
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (targetBot == null || e.Energy < targetBot.energy)
        {
            targetBot = new EnemyBot(this, e);
        }
    }

    private void AimAndFire(EnemyBot bot)
    {
        double bulletPower = 2;
        double bulletSpeed = 20 - (3 * bulletPower);
        double timeToHit = DistanceTo(bot.X, bot.Y) / bulletSpeed;

       
        double futureX = bot.X + bot.speed * timeToHit * Math.Cos(bot.direction * Math.PI / 180);
        double futureY = bot.Y + bot.speed * timeToHit * Math.Sin(bot.direction * Math.PI / 180);

        double angleToTarget = GunBearingTo(futureX, futureY);
        SetTurnGunRight(NormalizeRelativeAngle(angleToTarget));
        TurnGunLeft(angleToTarget);
        Fire(2);
    }

    public override void OnBotDeath(BotDeathEvent d)
    {
        if (d.VictimId == targetBot.id)
        {
            targetBot = null;
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        SetBack(100);
        SetTurnRight(90);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        SetTurnRight(90);
        SetForward(100);
    }
}
