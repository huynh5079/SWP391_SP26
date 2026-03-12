using EFCore.BulkExtensions;

namespace DataAccess.Entities;

public static class AEMSContextBulkExtensions
{
	public static Task BulkInsertAsync<TEntity>(this AEMSContext context, IList<TEntity> entities, CancellationToken cancellationToken = default)
		where TEntity : class
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(entities);

		if (entities.Count == 0)
		{
			return Task.CompletedTask;
		}

		context.PrepareBulkInsert(entities);

		BulkConfig? bulkConfig = null;
		if (typeof(BaseEntity).IsAssignableFrom(typeof(TEntity)))
		{
			bulkConfig = new BulkConfig
			{
				PropertiesToExclude = new List<string> { nameof(BaseEntity.RowVersion) }
			};
		}

		return DbContextBulkExtensions.BulkInsertAsync(
			context,
			entities,
			bulkConfig: bulkConfig,
			cancellationToken: cancellationToken);
	}
}
