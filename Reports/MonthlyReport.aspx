<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MonthlyReport.aspx.cs" Inherits="Reports_MonthlyReport" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Báo cáo hàng tháng</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .report-section-title { font-weight: 700; color: #1d2353; }
        .report-card { border-left: 4px solid #1d2353; }
        .stat-card .stat-title { font-size: 0.9rem; color: #6b7280; }
        .stat-card .stat-value { font-size: 1.4rem; font-weight: 700; color: #1d2353; }
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
        .country-tag {
            display: inline-block;
            padding: 2px 8px;
            border-radius: 999px;
            background: #eef2ff;
            color: #1e3a8a;
            border: 1px solid #dbeafe;
            font-size: 12px;
            margin: 2px 4px 2px 0;
            white-space: nowrap;
        }
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

    <h4 class="report-section-title mb-3">Báo cáo hàng tháng</h4>

    <div class="card report-card shadow-sm mb-3">
        <div class="card-body">
            <h6 class="report-section-title mb-3">1) Tổng quan kinh doanh</h6>
            <div class="row g-3">
                <div class="col-md-3">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Tổng booking</div>
                            <div class="stat-value"><asp:Literal ID="ltTotalBookings" runat="server" /></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Booking thành công</div>
                            <div class="stat-value"><asp:Literal ID="ltSuccessBookings" runat="server" /></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Doanh thu</div>
                            <div class="stat-value"><asp:Literal ID="ltTotalRevenue" runat="server" /></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Doanh thu/booking</div>
                            <div class="stat-value"><asp:Literal ID="ltRevenuePerBooking" runat="server" /></div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="mt-3 d-flex flex-wrap gap-2">
                <span class="badge-status badge-op">OP <asp:Literal ID="ltStatusOP" runat="server" /></span>
                <span class="badge-status badge-cx">CX <asp:Literal ID="ltStatusCX" runat="server" /></span>
                <span class="badge-status badge-bk">BK <asp:Literal ID="ltStatusBK" runat="server" /></span>
                <span class="badge-status badge-fp">FP <asp:Literal ID="ltStatusFP" runat="server" /></span>
            </div>
            <div class="mt-3">
                <h6 class="report-section-title">Top nguồn theo doanh thu</h6>
                <div class="table-responsive">
                    <table class="table table-sm mb-0">
                        <thead>
                            <tr>
                                <th>Nguồn</th>
                                <th class="text-end">Booking</th>
                                <th class="text-end">Doanh thu</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptTopSources" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("Source") %></td>
                                        <td class="text-end"><%# Eval("TotalBookings") %></td>
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

    <div class="card report-card shadow-sm mb-3">
        <div class="card-body">
            <h6 class="report-section-title mb-3">2) Hiệu quả nhân viên</h6>
            <div class="row g-3">
                <div class="col-md-4">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Tỷ lệ chuyển đổi</div>
                            <div class="stat-value"><asp:Literal ID="ltConversionRate" runat="server" /></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Booking quá hạn (CX)</div>
                            <div class="stat-value"><asp:Literal ID="ltOverdueCount" runat="server" /></div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="mt-3">
                <h6 class="report-section-title">Top nhân viên theo doanh thu</h6>
                <div class="table-responsive">
                    <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Nhân viên</th>
                                    <th class="text-end">Booking</th>
                                    <th class="text-end">Số khách</th>
                                    <th class="text-end">Doanh thu</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptTopStaff" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("StaffName") %></td>
                                            <td class="text-end"><%# Eval("TotalBookings") %></td>
                                            <td class="text-end"><%# Eval("TotalGuests") %></td>
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

    <div class="card report-card shadow-sm mb-3">
        <div class="card-body">
            <h6 class="report-section-title mb-3">3) Thị trường &amp; sản phẩm</h6>
            <div class="row g-3">
                <div class="col-md-6">
                    <h6 class="report-section-title">Top quốc gia theo doanh thu</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Thị trường</th>
                                    <th>Quốc gia</th>
                                    <th class="text-end">Booking</th>
                                    <th class="text-end">Doanh thu</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptTopCountries" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("Market") %></td>
                                            <td><asp:Literal ID="ltCountries" runat="server" Text='<%# Eval("CountriesHtml") %>' /></td>
                                            <td class="text-end"><%# Eval("TotalBookings") %></td>
                                            <td class="text-end"><%# Eval("Revenue") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="col-md-6">
                    <h6 class="report-section-title">Top ngày khởi hành</h6>
                    <div class="table-responsive">
                        <table class="table table-sm mb-0">
                            <thead>
                                <tr>
                                    <th>Ngày khởi hành</th>
                                    <th class="text-end">Booking</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptTopDepartures" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td class="text-nowrap"><%# Eval("DepartureDate") %></td>
                                            <td class="text-end"><%# Eval("TotalBookings") %></td>
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

    <div class="card report-card shadow-sm mb-3">
        <div class="card-body">
            <h6 class="report-section-title mb-3">4) Marketing &amp; giữ chân</h6>
            <div class="row g-3">
                <div class="col-md-4">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Khách mới</div>
                            <div class="stat-value"><asp:Literal ID="ltNewCustomers" runat="server" /></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card stat-card h-100">
                        <div class="card-body">
                            <div class="stat-title">Khách quay lại</div>
                            <div class="stat-value"><asp:Literal ID="ltReturningCustomers" runat="server" /></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="card report-card shadow-sm">
        <div class="card-body">
            <h6 class="report-section-title mb-3">5) Hành động &amp; rủi ro</h6>
            <div class="row g-3">
                <div class="col-md-6">
                    <h6 class="report-section-title">Danh sách quá hạn (CX)</h6>
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
                                <asp:Repeater ID="rptOverdueList" runat="server">
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
                <div class="col-md-6">
                    <h6 class="report-section-title">Deadline sắp tới (7 ngày)</h6>
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
                                <asp:Repeater ID="rptUpcomingList" runat="server">
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
</asp:Content>
