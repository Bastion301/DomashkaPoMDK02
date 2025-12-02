using System;
using System.Linq;

namespace WpfApp1
{
    public static class MaterialCalculator
    {
        /// <summary>
        /// Расчет количества материала, необходимого для производства продукции
        /// </summary>
        /// <param name="productTypeId">Идентификатор типа продукции</param>
        /// <param name="materialTypeId">Идентификатор типа материала</param>
        /// <param name="requiredProductQuantity">Требуемое количество продукции</param>
        /// <param name="productStockQuantity">Количество продукции на складе</param>
        /// <param name="productParameter1">Параметр продукции 1 (вещественное, положительное)</param>
        /// <param name="productParameter2">Параметр продукции 2 (вещественное, положительное)</param>
        /// <returns>Количество необходимого материала или -1 при ошибке</returns>
        public static int CalculateRequiredMaterial(
            int productTypeId,
            int materialTypeId,
            int requiredProductQuantity,
            int productStockQuantity,
            double productParameter1,
            double productParameter2)
        {
            try
            {
                // Проверка входных параметров
                if (productTypeId <= 0 || materialTypeId <= 0 ||
                    requiredProductQuantity <= 0 || productStockQuantity < 0 ||
                    productParameter1 <= 0 || productParameter2 <= 0)
                {
                    return -1;
                }

                using (var db = new PartnerOrdersEntities1())
                {
                    // Проверка существования типов продукции и материалов
                    var productType = db.ProductTypes.FirstOrDefault(pt => pt.ProductTypeID == productTypeId);
                    var materialType = db.MaterialTypes.FirstOrDefault(mt => mt.MaterialTypeID == materialTypeId);

                    if (productType == null || materialType == null)
                    {
                        return -1;
                    }

                    // Рассчитываем количество продукции, которое нужно произвести
                    int productionQuantity = Math.Max(0, requiredProductQuantity - productStockQuantity);
                    if (productionQuantity == 0)
                    {
                        return 0; // Вся продукция уже есть на складе
                    }

                    // Количество материала на одну единицу продукции
                    // (произведение параметров, умноженное на коэффициент типа продукции)
                    double materialPerUnit = productParameter1 * productParameter2 * (double)productType.ProductTypeCoefficient;

                    // Учитываем процент брака материала
                    double materialWithDefect = materialPerUnit * (1 + (double)materialType.DefectPercentage);

                    // Общее количество материала с учетом брака
                    double totalMaterial = materialWithDefect * productionQuantity;

                    // Округляем вверх до целого числа
                    return (int)Math.Ceiling(totalMaterial);
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}