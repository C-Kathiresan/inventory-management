using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Text.Json;

namespace Inventory.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _repo;
        private readonly IDistributedCache _cache;
        public ProductController(IProductRepository repo, IDistributedCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        [HttpGet]
        [Route("getproducts")]
        public async Task<IActionResult> GetProducts()
        {
            const string cacheKey = "productList";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                // Deserialize JSON to List<ProductDto>
                var productsFromCache = JsonSerializer.Deserialize<List<Product>>(cachedData);
                return Ok(productsFromCache ?? new List<Product>());
            }
            var getpro = await _repo.GetAllAsync();
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            var serializedData = JsonSerializer.Serialize(getpro);
            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);

            return Ok(getpro);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            return product == null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product)
        {
            _cache.RemoveAsync("productList");
            await _repo.AddAsync(product);
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Update(Product product)
        {
            await _repo.UpdateAsync(product);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return Ok();
        }
    }
}
