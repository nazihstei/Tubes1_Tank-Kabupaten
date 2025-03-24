using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ----------------- Additional Class: EnemyBot -----------------
public sealed class EnemyBot {
    public const int UNKNOWN = -999;
    public int id { get; set; } 
    public double energy { get; set; }
    public double X { get; set; } 
    public double Y { get; set; }
    public double distance { get; set; }
    public double direction { get; set; }
    public double speed { get; set; }
    public EnemyBot(EnemyBot e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        this.id = e.id;
        this.energy = e.energy;
        this.X = e.X;
        this.Y = e.Y;
        this.distance = e.distance;
        this.direction = e.direction;
        this.speed = e.speed;
    }
    public EnemyBot(Bot MyBot, ScannedBotEvent e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        this.id = e.ScannedBotId;
        this.energy = e.Energy;
        this.X = e.X;
        this.Y = e.Y;
        this.distance = MyBot.DistanceTo(e.X, e.Y);
        this.direction = e.Direction;
        this.speed = e.Speed;
    }
    public EnemyBot(Bot MyBot, HitBotEvent e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        this.id = e.VictimId;
        this.energy = e.Energy;
        this.X = e.X;
        this.Y = e.Y;
        this.distance = MyBot.DistanceTo(e.X, e.Y);
        this.direction = UNKNOWN;
        this.speed = UNKNOWN;
    }
    public EnemyBot(HitByBulletEvent e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        var enemyPosition = predictPositionFromBullet(e.Bullet.X, e.Bullet.Y, e.Bullet.Speed, e.Bullet.Direction, e.Bullet.Power);
        this.id = e.Bullet.OwnerId;
        this.energy = UNKNOWN;
        this.X = enemyPosition.X;
        this.Y = enemyPosition.Y;
        this.distance = enemyPosition.distance;
        this.direction = UNKNOWN;
        this.speed = UNKNOWN;
    }
    public EnemyBot(Bot MyBot, BulletHitBotEvent e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        this.id = e.VictimId;
        this.energy = e.Energy;
        this.X = e.Bullet.X;
        this.Y = e.Bullet.Y;
        this.distance = MyBot.DistanceTo(this.X, this.Y);
        this.direction = UNKNOWN;
        this.speed = UNKNOWN;
    }
    public EnemyBot(BulletHitBulletEvent e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        var enemyPosition = predictPositionFromBullet(e.Bullet.X, e.Bullet.Y, e.Bullet.Speed, e.Bullet.Direction, e.Bullet.Power);
        this.id = e.HitBullet.OwnerId;
        this.energy = UNKNOWN;
        this.X = enemyPosition.X;
        this.Y = enemyPosition.Y;
        this.distance = enemyPosition.distance;
        this.direction = UNKNOWN;
        this.speed = UNKNOWN;
    }
    public static (double X, double Y, double distance) predictPositionFromBullet(double baseX, double baseY, double speed, double direction, double power, double delay=7) {
        // default delay = 7 (asumsi ditembakkan 5-10 tick yg lalu)
        double bulletSpeed = 20 - 3 * power;
        double estimatedDistance = bulletSpeed * delay;
        double shooterX = baseX - estimatedDistance * Math.Cos(Math.PI / 180 * direction);
        double shooterY = baseY - estimatedDistance * Math.Sin(Math.PI / 180 * direction);
        return (shooterX, shooterY, estimatedDistance);
    }

}
