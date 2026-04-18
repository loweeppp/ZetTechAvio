const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

/**
 * Создать платеж для бронирования
 * @param {number} bookingId - ID бронирования
 * @param {string} token - JWT токен
 * @returns {object} {confirmationUrl, paymentId, amount, status}
 */
export const createPayment = async (bookingId, token) => {
  const response = await fetch(`${API_URL}/api/payment/create-payment`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ bookingId }),
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || 'Ошибка при создании платежа');
  }

  const data = await response.json();
  return data;
};

/**
 * Получить информацию о платеже
 * @param {number} paymentId - ID платежа
 * @param {string} token - JWT токен
 * @returns {object} полная информация о платеже
 */
export const getPayment = async (paymentId, token) => {
  const response = await fetch(`${API_URL}/api/payment/${paymentId}`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || 'Ошибка при получении платежа');
  }

  const data = await response.json();
  return data;
};

/**
 * Проверить статус платежа и обновить его
 * Используется после возврата с YooKassa
 * @param {number} bookingId - ID бронирования
 * @param {string} yooKassaPaymentId - ID платежа в YooKassa
 * @param {string} token - JWT токен
 * @returns {object} обновленная информация о платеже
 */
export const verifyPaymentStatus = async (bookingId, yooKassaPaymentId, token) => {
  const response = await fetch(`${API_URL}/api/payment/verify-status`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ bookingId, yooKassaPaymentId }),
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || 'Ошибка при проверке статуса платежа');
  }

  const data = await response.json();
  return data;
};

export default {
  createPayment,
  getPayment,
};
