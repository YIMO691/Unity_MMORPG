using MmoDemo.Domain;

namespace MmoDemo.Application;

public interface IMovementService
{
    void ValidateAndApply(Entity entity, float dirX, float dirZ, float clientPosX, float clientPosZ);
}
