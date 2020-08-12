using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Entities;
using ECommerce.Core.Entities.Orders;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Specifications;

namespace ECommerce.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _unitOfWork;
        public OrderService(IUnitOfWork unitOfWork, IBasketRepository basketRepository)
        {
            _unitOfWork = unitOfWork;
            _basketRepository = basketRepository;
        }

        public async Task<Order> CreateOrderAsync(string userName, int deliveryMethodId, string basketId, Adress shippingAdress)
        {
            // get basket from the basket repo
            var basket = await _basketRepository.GetBasketAsync(basketId);

            // get delivery from delivery repo
            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);

            // create order
            var order = new Order(userName, shippingAdress, deliveryMethod);
            
            // get product from product repo
            foreach (var basketItem in basket.Items)
            {
                var productItem = await _unitOfWork.Repository<Product>().GetByIdAsync(basketItem.Id);

                var itemOrdered = new ProductItemOrdered(productItem.Id,
                productItem.Name, productItem.PictureUrl);

                order.AddOrderItem(itemOrdered, productItem.Price, basketItem.Quantity);
            }

            // TODO: save db
            _unitOfWork.Repository<Order>().Add(order);
            var result = await _unitOfWork.SaveAsync(new System.Threading.CancellationToken());

            if(result < 0) return null;

            await _basketRepository.DeleteBasket(basketId);
            
            // return order
            return order;
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await _unitOfWork.Repository<DeliveryMethod>().ListAllAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id, string userName)
        {
            OrderWithItemsAndOrederingSpecification spec = new OrderWithItemsAndOrederingSpecification(id, userName);

            return await _unitOfWork.Repository<Order>().GetEntityWithSpecAsync(spec);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByUserNameAsync(string userName)
        {
            OrderWithItemsAndOrederingSpecification spec = new OrderWithItemsAndOrederingSpecification(userName);
            
            return await  _unitOfWork.Repository<Order>().ListAsync(spec);
        }
    }
}