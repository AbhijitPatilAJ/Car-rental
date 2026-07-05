// search.js — Search form logic and results rendering

const form    = document.getElementById('search-form');
const results = document.getElementById('results-section');
const stateEl = document.getElementById('state-container');
const sortBtn = document.getElementById('btn-sort');
const countEl = document.getElementById('results-count-text');

let currentResults = [];
let currentPickup  = '';
let sortAsc        = true;

// Set min date to today
const today = new Date().toISOString().split('T')[0];
document.getElementById('from-date').min = today;
document.getElementById('to-date').min   = today;

document.getElementById('from-date').addEventListener('change', e => {
  document.getElementById('to-date').min = e.target.value;
});

form.addEventListener('submit', async e => {
  e.preventDefault();
  await performSearch();
});

sortBtn.addEventListener('click', () => {
  sortAsc = !sortAsc;
  sortBtn.textContent = sortAsc ? '↑ Sort by Total Price' : '↓ Sort by Total Price';
  renderCards(currentResults.slice().sort((a, b) =>
    sortAsc ? a.totalPrice - b.totalPrice : b.totalPrice - a.totalPrice
  ));
});

async function performSearch() {
  const pickup   = document.getElementById('pickup').value.trim();
  const from     = document.getElementById('from-date').value;
  const to       = document.getElementById('to-date').value;
  const category = document.getElementById('category').value;

  if (!pickup || !from || !to) return;
  if (to <= from) { showError('Return date must be after pickup date.'); return; }

  currentPickup = pickup;
  showLoading();
  sortBtn.classList.add('hidden');

  try {
    const data = await api.search({ pickup, from, to, category: category || null });
    currentResults = data;
    sortAsc = true;
    sortBtn.textContent = '↑ Sort by Total Price';

    if (data.length === 0) {
      showEmpty();
    } else {
      renderCards(data);
      const pickupType = classifyCity(pickup);
      const currencyNote = pickupType === 'international'
        ? ' <span class="currency-note">· Prices shown in ₹ INR and $ USD</span>'
        : ' <span class="currency-note">· Prices shown in ₹ INR</span>';
      countEl.innerHTML = `Found <strong>${data.length}</strong> vehicle${data.length !== 1 ? 's' : ''}${currencyNote}`;
      document.getElementById('results-controls').classList.remove('hidden');
      sortBtn.classList.remove('hidden');
    }
  } catch (err) {
    showError(err.message || 'Failed to search. Make sure the backend is running on http://localhost:5000');
  }
}

function showLoading() {
  results.innerHTML = '';
  document.getElementById('results-controls').classList.add('hidden');
  stateEl.innerHTML = `
    <div class="state-container">
      <div class="spinner"></div>
      <div class="state-title">Searching providers…</div>
      <div class="state-subtitle">Checking PremiumDrive and BudgetWheels</div>
    </div>`;
}

function showEmpty() {
  document.getElementById('results-controls').classList.add('hidden');
  stateEl.innerHTML = `
    <div class="state-container">
      <div class="state-icon">🚗</div>
      <div class="state-title">No vehicles available</div>
      <div class="state-subtitle">Try different dates, location, or category</div>
    </div>`;
  results.innerHTML = '';
}

function showError(msg) {
  stateEl.innerHTML = `
    <div class="error-banner">
      <span>⚠️</span>
      <span>${msg}</span>
    </div>`;
  results.innerHTML = '';
}

function renderCards(data) {
  const pickupType = classifyCity(currentPickup);
  stateEl.innerHTML = '';
  results.innerHTML = `<div class="results-grid">${data.map(v => buildCard(v, pickupType)).join('')}</div>`;

  document.querySelectorAll('.btn-book').forEach(btn => {
    btn.addEventListener('click', () => {
      const vehicle = data.find(v => v.vehicleId === btn.dataset.id);
      sessionStorage.setItem('selectedVehicle', JSON.stringify(vehicle));
      const q = new URLSearchParams({
        pickup: currentPickup,
        from:   document.getElementById('from-date').value,
        to:     document.getElementById('to-date').value,
      });
      window.location.href = `booking.html?${q}`;
    });
  });
}

// ── Weekend night counter (mirrors backend BudgetWheels logic) ────────────────
function countNightTypes(fromStr, toStr) {
  let weekendNights = 0, weekdayNights = 0;
  let cur = new Date(fromStr + 'T00:00:00');
  const end = new Date(toStr + 'T00:00:00');
  while (cur < end) {
    const day = cur.getDay(); // 0=Sun, 5=Fri, 6=Sat
    if (day === 0 || day === 5 || day === 6) weekendNights++;
    else weekdayNights++;
    cur.setDate(cur.getDate() + 1);
  }
  return { weekendNights, weekdayNights };
}

function buildCard(v, pickupType) {
  const isPremium  = v.provider === 'PremiumDrive';
  const badgeCls   = isPremium ? 'badge-premium' : 'badge-budget';
  const badgeIcon  = isPremium ? '⭐' : '💰';

  const cancelChip = isPremium
    ? `<span class="detail-chip chip-success">✓ Free cancel 48h</span>`
    : `<span class="detail-chip chip-danger">✗ Non-refundable</span>`;
  const insurChip = isPremium
    ? `<span class="detail-chip chip-success">🛡 Comprehensive</span>`
    : `<span class="detail-chip chip-warning">🛡 Basic</span>`;

  const displayName = v.vehicleName || v.category;

  // ── Currency display ──────────────────────────────────────────────────────
  const totalDisplay = pickupType === 'international'
    ? `${formatINR(v.totalPrice)} <span class="usd-price">≈ ${formatUSD(inrToUsd(v.totalPrice))}</span>`
    : formatINR(v.totalPrice);

  const dailyDisplay = pickupType === 'international'
    ? `${formatINR(v.dailyRate)} <span class="usd-price">≈ ${formatUSD(inrToUsd(v.dailyRate))}</span>`
    : formatINR(v.dailyRate);

  const currencyTag = pickupType === 'international'
    ? `<span class="currency-tag intl-tag">₹ INR · $ USD</span>`
    : `<span class="currency-tag domestic-tag">₹ INR</span>`;

  // ── Pricing breakdown panel ───────────────────────────────────────────────
  let surchargeInfo = '';
  const fromVal = document.getElementById('from-date').value;
  const toVal   = document.getElementById('to-date').value;

  if (!isPremium && fromVal && toVal) {
    // BudgetWheels — show night-by-night surcharge breakdown
    const { weekendNights, weekdayNights } = countNightTypes(fromVal, toVal);
    const effectiveRate = v.rentalDays > 0
      ? formatINR(Math.round(v.totalPrice / v.rentalDays))
      : formatINR(v.dailyRate);

    if (weekendNights > 0) {
      // Mixed or all-weekend → show surcharge rows
      surchargeInfo = `
        <div class="surcharge-box">
          ${weekdayNights > 0 ? `
          <div class="surcharge-row">
            <span>🌙 ${weekdayNights} weeknight${weekdayNights !== 1 ? 's' : ''}</span>
            <span>${formatINR(v.dailyRate)}/night</span>
          </div>` : ''}
          <div class="surcharge-row weekend-row">
            <span>🎉 ${weekendNights} weekend night${weekendNights !== 1 ? 's' : ''} <em>(+20% surcharge)</em></span>
            <span>${formatINR(Math.round(v.dailyRate * 1.2))}/night</span>
          </div>
          <div class="surcharge-row effective-row">
            <span>📊 Avg per night</span>
            <span>${effectiveRate}</span>
          </div>
        </div>`;
    } else {
      // All weekdays — no surcharge
      surchargeInfo = `
        <div class="surcharge-box no-surcharge">
          <div class="surcharge-row">
            <span>🌙 ${weekdayNights} weeknight${weekdayNights !== 1 ? 's' : ''} — no weekend surcharge</span>
            <span>${formatINR(v.dailyRate)}/night</span>
          </div>
        </div>`;
    }
  } else if (isPremium && fromVal && toVal) {
    // PremiumDrive — flat rate, no surcharge ever
    surchargeInfo = `
      <div class="surcharge-box flat-rate">
        <div class="surcharge-row">
          <span>📋 Flat rate — same price every night</span>
          <span>${formatINR(v.dailyRate)}/night</span>
        </div>
      </div>`;
  }

  return `
    <div class="vehicle-card">
      <div class="card-header">
        <div style="flex:1;">
          <div class="provider-badge ${badgeCls}">${badgeIcon} ${v.provider}</div>
          <div class="vehicle-name">${displayName}</div>
          <div style="display:flex;align-items:center;gap:8px;margin-top:6px;flex-wrap:wrap;">
            <span class="cat-pill">${v.category}</span>
            <span class="vid-pill">${v.vehicleId}</span>
            ${currencyTag}
          </div>
        </div>
      </div>
      <div class="pricing-section">
        <div>
          <div class="price-total">${totalDisplay} <span>total</span></div>
          <div class="rental-nights">${v.rentalDays} night${v.rentalDays !== 1 ? 's' : ''}</div>
        </div>
        <div class="price-daily">
          <strong>${dailyDisplay}</strong><br>base/night
        </div>
      </div>
      ${surchargeInfo}
      <div class="details-row">
        ${cancelChip}
        ${insurChip}
      </div>
      <button class="btn-book" data-id="${v.vehicleId}" id="book-${v.vehicleId.replace(/[^a-zA-Z0-9]/g, '')}">
        Book Now →
      </button>
    </div>`;
}
