using MmoDemo.Domain;

namespace MmoDemo.Application;

public class MovementService : IMovementService
{
    private const float MaxSpeedMultiplier = 1.5f;
    private const float MaxPositionDelta = 2f; // max allowed deviation before correction

    public void ValidateAndApply(Entity entity, float dirX, float dirZ, float clientPosX, float clientPosZ)
    {
        // Clamp direction to prevent speed hacks
        var magnitude = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
        if (magnitude > 1f)
        {
            dirX /= magnitude;
            dirZ /= magnitude;
        }

        // Calculate server-authoritative position
        // Assume 100ms tick — delta is direction * speed * tickTime
        const float tickTime = 0.1f;
        var deltaX = dirX * entity.MoveSpeed * tickTime;
        var deltaZ = dirZ * entity.MoveSpeed * tickTime;

        var serverX = entity.PosX + deltaX;
        var serverZ = entity.PosZ + deltaZ;

        // Anti-cheat: if client position is too far from server position, force correction
        var distFromServer = MathF.Sqrt(
            (clientPosX - serverX) * (clientPosX - serverX) +
            (clientPosZ - serverZ) * (clientPosZ - serverZ));

        if (distFromServer > MaxPositionDelta)
        {
            // Client drifted too far — use server position (correction)
            entity.PosX = serverX;
            entity.PosZ = serverZ;
        }
        else
        {
            // Accept client position (feels smoother)
            entity.PosX = clientPosX;
            entity.PosZ = clientPosZ;
        }

        // Update rotation based on direction
        if (magnitude > 0.01f)
        {
            entity.RotY = MathF.Atan2(dirX, dirZ) * (180f / MathF.PI);
        }
    }
}
