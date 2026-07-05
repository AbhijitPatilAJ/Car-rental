// Central API configuration — change API_BASE if you change the port
const API_BASE = 'http://localhost:5000';

// ── Exchange Rate (demo fixed rate — replace with live API in production) ────
const USD_RATE = 84.00; // 1 USD = ₹84 INR

const api = {
  async search({ pickup, from, to, category }) {
    let url = `${API_BASE}/cars/search?pickup=${encodeURIComponent(pickup)}&from=${from}&to=${to}`;
    if (category) url += `&category=${category}`;
    const res  = await fetch(url);
    const data = await res.json();
    if (!res.ok) throw new ApiError(data.error || 'Search failed', res.status);
    return data;
  },

  async book(payload) {
    const res  = await fetch(`${API_BASE}/cars/book`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) throw new ApiError(data.message || data.error || 'Booking failed', res.status, data);
    return data;
  },

  async getBooking(reference) {
    const res  = await fetch(`${API_BASE}/cars/booking/${encodeURIComponent(reference)}`);
    const data = await res.json();
    if (!res.ok) throw new ApiError(data.error || 'Not found', res.status);
    return data;
  },
};

class ApiError extends Error {
  constructor(message, status, body = null) {
    super(message);
    this.status = status;
    this.body   = body;
  }
}

// ── City classification — mirrors server-side CityRegistry ───────────────────
const DOMESTIC_CITIES     = ['Bangalore', 'Mumbai', 'Delhi'];
const INTERNATIONAL_CITIES = ['Paris', 'Dubai', 'New York', 'Tokyo', 'Sydney'];

function classifyCity(city) {
  if (DOMESTIC_CITIES.some(c => c.toLowerCase() === city.toLowerCase()))      return 'domestic';
  if (INTERNATIONAL_CITIES.some(c => c.toLowerCase() === city.toLowerCase())) return 'international';
  return null;
}

function validateDocument(pickup, documentType) {
  const type = classifyCity(pickup);
  if (type === 'international' && documentType !== 'Passport') {
    return `"${pickup}" is an international location. A Passport is required.`;
  }
  return null;
}

// ── Currency utilities ────────────────────────────────────────────────────────

/** Format amount in INR (₹) */
function formatINR(amount) {
  return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount);
}

/** Format amount in USD ($) */
function formatUSD(amount) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(amount);
}

/** Convert INR amount to USD */
function inrToUsd(inrAmount) {
  return Math.round((inrAmount / USD_RATE) * 100) / 100;
}

/**
 * Format an INR price in the chosen currency.
 * @param {number} inrAmount - price in INR (base)
 * @param {'INR'|'USD'} currency
 */
function formatPrice(inrAmount, currency = 'INR') {
  if (currency === 'USD') return formatUSD(inrToUsd(inrAmount));
  return formatINR(inrAmount);
}

/**
 * Build a dual-price string: "₹1,400 / $16" for international pickups.
 * Returns only INR for domestic pickups.
 */
function formatDualPrice(inrAmount, pickupType) {
  if (pickupType === 'international') {
    return `${formatINR(inrAmount)} <span class="usd-price">≈ ${formatUSD(inrToUsd(inrAmount))}</span>`;
  }
  return formatINR(inrAmount);
}

function formatDate(dateStr) {
  return new Date(dateStr + 'T00:00:00').toLocaleDateString('en-IN', {
    weekday: 'short', day: 'numeric', month: 'short', year: 'numeric'
  });
}
