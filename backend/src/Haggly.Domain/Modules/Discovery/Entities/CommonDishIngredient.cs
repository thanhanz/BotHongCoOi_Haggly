namespace Haggly.Domain.Modules.Discovery;

public sealed class CommonDishIngredient
{
    private CommonDishIngredient()
    {
    }

    public CommonDishIngredient(Guid dishId, Guid canonicalIngredientId)
    {
        if (dishId == Guid.Empty || canonicalIngredientId == Guid.Empty)
        {
            throw new ArgumentException("Valid dish and ingredient IDs are required.");
        }

        DishId = dishId;
        CanonicalIngredientId = canonicalIngredientId;
    }

    public Guid DishId { get; private set; }
    public Guid CanonicalIngredientId { get; private set; }
    public CommonDish? Dish { get; private set; }
    public CanonicalIngredient? CanonicalIngredient { get; private set; }
}
