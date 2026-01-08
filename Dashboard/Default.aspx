<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Dashboard</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
    <style>
        .stat-card .stat-title {
            font-size: 0.9rem;
            color: #6b7280;
        }
        .stat-card .stat-value {
            font-size: 1.6rem;
            font-weight: 700;
            color: #1d2353;
        }
        .badge-status {
            display: inline-block;
            padding: 2px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
            border: 1px solid transparent;
            min-width: 36px;
            text-align: center;
        }
        .badge-op { background: #e2e8f0; color: #1f2937; border-color: #cbd5e1; }
        .badge-cx { background: #fee2e2; color: #991b1b; border-color: #fecaca; }
        .badge-bk { background: #fef3c7; color: #92400e; border-color: #fde68a; }
        .badge-fp { background: #dcfce7; color: #166534; border-color: #bbf7d0; }
        .table-sm td, .table-sm th { vertical-align: middle; }
        .text-nowrap { white-space: nowrap; }
        .chart-box { height: 220px; }
        .chart-box-wide { height: 260px; }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm mb-3">
        <div class="card-body">
            <div class="row g-2 align-items-end">
                <div class="col-sm-3 col-md-2">
                    <label class="form-label fw-semibold">Từ ngày</label>
                    <asp:TextBox ID="txtFromDate" runat="server" TextMode="Date" CssClass="form-control" />
                </div>
                <div class="col-sm-3 col-md-2">
                    <label class="form-label fw-semibold">Đến ngày</label>
                    <asp:TextBox ID="txtToDate" runat="server" TextMode="Date" CssClass="form-control" />
                </div>
                <div class="col-sm-6 col-md-4 d-flex gap-2">
                    <asp:Button ID="btnApplyFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" OnClick="btnApplyFilter_Click" />
                    <asp:Button ID="btnClearFilter" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" OnClick="btnClearFilter_Click" />
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3">
        <asp:Literal ID="ltBookingsWeek" runat="server" Visible="false" />
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Booking hôm nay</div>
                    <div class="stat-value"><asp:Literal ID="ltBookingsToday" runat="server" /></div>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Doanh thu hôm nay</div>
                    <div class="stat-value"><asp:Literal ID="ltRevenueToday" runat="server" /></div>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Booking tháng</div>
                    <div class="stat-value"><asp:Literal ID="ltBookingsMonth" runat="server" /></div>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Doanh thu tháng</div>
                    <div class="stat-value"><asp:Literal ID="ltRevenueMonth" runat="server" /></div>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Tỷ lệ FP tháng</div>
                    <div class="stat-value"><asp:Literal ID="ltFpRateMonth" runat="server" /></div>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm stat-card h-100">
                <div class="card-body">
                    <div class="stat-title">Booking quá hạn</div>
                    <div class="stat-value"><asp:Literal ID="ltOverdueCount" runat="server" /></div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mt-1">
        <div class="col-md-7">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Doanh thu theo ngày</h6>
                    <div class="chart-box">
                        <canvas id="revenueTrendChart"></canvas>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-5">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Cơ cấu trạng thái thanh toán</h6>
                    <div class="chart-box">
                        <canvas id="paymentStatusChart"></canvas>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mt-1">
        <div class="col-md-12">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Booking theo nguồn</h6>
                    <div class="chart-box chart-box-wide">
                        <canvas id="sourceBarChart"></canvas>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mt-1">
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <div class="d-flex align-items-center justify-content-between mb-2">
                        <h6 class="mb-0">Tình trạng thanh toán</h6>
                        <span class="text-muted small">Tổng khách: <asp:Literal ID="ltTotalCustomers" runat="server" /></span>
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                        <span class="badge-status badge-op">OP <asp:Literal ID="ltStatusOP" runat="server" /></span>
                        <span class="badge-status badge-cx">CX <asp:Literal ID="ltStatusCX" runat="server" /></span>
                        <span class="badge-status badge-bk">BK <asp:Literal ID="ltStatusBK" runat="server" /></span>
                        <span class="badge-status badge-fp">FP <asp:Literal ID="ltStatusFP" runat="server" /></span>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Khách mới / quay lại (tháng)</h6>
                    <div class="row">
                        <div class="col-6">
                            <div class="text-muted small">Khách mới</div>
                            <div class="stat-value"><asp:Literal ID="ltNewCustomersMonth" runat="server" /></div>
                        </div>
                        <div class="col-6">
                            <div class="text-muted small">Khách quay lại</div>
                            <div class="stat-value"><asp:Literal ID="ltReturningCustomersMonth" runat="server" /></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Đơn mới chưa xử lý hôm nay</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Booking</th>
                                    <th>Khách</th>
                                    <th class="text-end">Ngày</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptPendingToday" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="text-nowrap"><%# Eval("OrderId") %></td>
                                            <td><%# Eval("CustomerName") %></td>
                                            <td class="text-end text-nowrap"><%# Eval("CreatedDate") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mt-1">
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Top nguồn booking</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Nguồn</th>
                                    <th class="text-end">Số</th>
                                    <th class="text-end">Doanh thu</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptSources" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("Source") %></td>
                                            <td class="text-end"><%# Eval("Total") %></td>
                                            <td class="text-end"><%# Eval("Revenue") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Top nhân viên tạo booking</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Nhân viên</th>
                                    <th class="text-end">Số</th>
                                    <th class="text-end">Doanh thu</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptCreators" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("CreatedBy") %></td>
                                            <td class="text-end"><%# Eval("Total") %></td>
                                            <td class="text-end"><%# Eval("Revenue") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Booking quá hạn (CX)</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Booking</th>
                                    <th>Khách</th>
                                    <th class="text-end">Hạn</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptOverdue" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="text-nowrap"><%# Eval("OrderId") %></td>
                                            <td><%# Eval("CustomerName") %></td>
                                            <td class="text-end text-nowrap"><%# Eval("DepositDeadline") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mt-1">
        <div class="col-md-7">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Booking theo 7 ngày gần nhất</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Ngày</th>
                                    <th class="text-end">Số booking</th>
                                    <th class="text-end">Doanh thu</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptTrend" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="text-nowrap"><%# Eval("Date") %></td>
                                            <td class="text-end"><%# Eval("Total") %></td>
                                            <td class="text-end"><%# Eval("Revenue") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-5">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <h6 class="mb-3">Deadline thanh toán sắp tới (7 ngày)</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Booking</th>
                                    <th>Khách</th>
                                    <th class="text-end">Hạn</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptDeadlines" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="text-nowrap"><%# Eval("OrderId") %></td>
                                            <td><%# Eval("CustomerName") %></td>
                                            <td class="text-end text-nowrap"><%# Eval("DepositDeadline") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        (function () {
            var trendLabels = <asp:Literal ID="ltChartTrendLabels" runat="server" />;
            var trendRevenue = <asp:Literal ID="ltChartTrendRevenue" runat="server" />;
            var sourceLabels = <asp:Literal ID="ltChartSourceLabels" runat="server" />;
            var sourceCounts = <asp:Literal ID="ltChartSourceCounts" runat="server" />;
            var statusLabels = <asp:Literal ID="ltChartStatusLabels" runat="server" />;
            var statusCounts = <asp:Literal ID="ltChartStatusCounts" runat="server" />;

            var trendEl = document.getElementById('revenueTrendChart');
            if (trendEl) {
                new Chart(trendEl, {
                    type: 'line',
                    data: {
                        labels: trendLabels,
                        datasets: [{
                            label: 'Doanh thu',
                            data: trendRevenue,
                            borderColor: '#1d4ed8',
                            backgroundColor: 'rgba(29, 78, 216, 0.12)',
                            fill: true,
                            tension: 0.3,
                            pointRadius: 3
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { ticks: { callback: function (value) { return value.toLocaleString('vi-VN'); } } }
                        }
                    }
                });
            }

            var sourceEl = document.getElementById('sourceBarChart');
            if (sourceEl) {
                new Chart(sourceEl, {
                    type: 'bar',
                    data: {
                        labels: sourceLabels,
                        datasets: [{
                            label: 'Booking',
                            data: sourceCounts,
                            backgroundColor: '#60a5fa'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { ticks: { callback: function (value) { return value.toLocaleString('vi-VN'); } } }
                        }
                    }
                });
            }

            var statusEl = document.getElementById('paymentStatusChart');
            if (statusEl) {
                new Chart(statusEl, {
                    type: 'doughnut',
                    data: {
                        labels: statusLabels,
                        datasets: [{
                            data: statusCounts,
                            backgroundColor: ['#e2e8f0', '#fee2e2', '#fef3c7', '#dcfce7'],
                            borderWidth: 1
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { position: 'bottom' } }
                    }
                });
            }
        })();
    </script>
</asp:Content>
