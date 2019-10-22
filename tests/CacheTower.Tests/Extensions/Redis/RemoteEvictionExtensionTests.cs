﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheTower.Extensions.Redis;
using CacheTower.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CacheTower.Tests.Extensions.Redis
{
	[TestClass]
	public class RemoteEvictionExtensionTests
	{
		[TestMethod, ExpectedException(typeof(ArgumentNullException))]
		public void ThrowForNullConnection()
		{
			new RemoteEvictionExtension(null);
		}

		[TestMethod, ExpectedException(typeof(ArgumentNullException))]
		public void ThrowForNullChannel()
		{
			new RemoteEvictionExtension(RedisHelper.GetConnection(), null);
		}

		[TestMethod, ExpectedException(typeof(InvalidOperationException))]
		public void ThrowForRegisteringMoreThanOneCacheStack()
		{
			var extension = new RemoteEvictionExtension(RedisHelper.GetConnection());
			var cacheStackMock = new Mock<ICacheStack>();
			extension.Register(cacheStackMock.Object);
			extension.Register(cacheStackMock.Object);
		}

		[TestMethod]
		public async Task EvictsFromChannelButNotFromRegisteredCacheStack()
		{
			var connection = RedisHelper.GetConnection();
			var hasMessagedSubscribers = false;

			await connection.GetSubscriber().SubscribeAsync("CacheTower.RemoteEviction", (channel, value) =>
			{
				if (value == "TestKey")
				{
					hasMessagedSubscribers = true;
				}
			});

			var cacheStackMock = new Mock<ICacheStack>();
			var extension = new RemoteEvictionExtension(connection);
			extension.Register(cacheStackMock.Object);

			await extension.OnValueRefreshAsync(string.Empty, "TestKey", TimeSpan.FromDays(1));
			await Task.Delay(1000);

			Assert.IsTrue(hasMessagedSubscribers, "Subscribers were not notified about the refreshed value");
			cacheStackMock.Verify(c => c.EvictAsync("TestKey"), Times.Never, "The CacheStack that published the refresh was told to evict its own cache");
		}
	}
}
