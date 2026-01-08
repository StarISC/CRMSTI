<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SourceByCustomer.aspx.cs" Inherits="SourceByCustomer" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Báo cáo - Theo nguồn khách</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .report-card { border-radius: 12px; }
        .kpi { font-weight: 700; font-size: 22px; color: #1d2353; }
        .kpi-label { color: #6b7280; font-size: 13px; }
        #sourceByCustomerTable_wrapper .dataTables_length { display: none; }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm report-card mb-3">
        <div class="card-body">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <h2 class="h4 mb-1 text-primary">Báo cáo theo nguồn khách</h2>
                    <div class="text-muted">Tổng hợp booking theo nguồn khách trong khoảng thời gian</div>
                </div>
            </div>
        </div>
    </div>

    <div class="row g-3 mb-3">
        <div class="col-md-3">
            <div class="card shadow-sm report-card">
                <div class="card-body">
                    <div class="kpi" id="kpiSources">0</div>
                    <div class="kpi-label">Nguồn khách</div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card shadow-sm report-card">
                <div class="card-body">
                    <div class="kpi" id="kpiBookings">0</div>
                    <div class="kpi-label">Booking</div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card shadow-sm report-card">
                <div class="card-body">
                    <div class="kpi" id="kpiGuests">0</div>
                    <div class="kpi-label">Số khách</div>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card shadow-sm report-card">
                <div class="card-body">
                    <div class="kpi" id="kpiRevenue">0</div>
                    <div class="kpi-label">Tổng thực bán</div>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm report-card mb-3">
        <div class="card-body">
            <div class="row g-2 align-items-end">
                <div class="col-md-3">
                    <label class="form-label fw-semibold">Từ ngày</label>
                    <input type="date" id="fromDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <label class="form-label fw-semibold">Đến ngày</label>
                    <input type="date" id="toDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <label class="form-label fw-semibold">Từ khóa nguồn</label>
                    <input type="text" id="sourceKeyword" class="form-control" placeholder="Nguồn khách" />
                </div>
                <div class="col-md-3 d-flex gap-2">
                    <button type="button" id="btnFilter" class="btn btn-primary">Lọc</button>
                    <button type="button" id="btnReset" class="btn btn-outline-secondary">Xóa lọc</button>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm report-card">
        <div class="card-body">
            <div class="table-responsive">
                <table id="sourceByCustomerTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Nguồn khách</th>
                            <th>Số booking</th>
                            <th>Số khách</th>
                            <th>Tổng tiền</th>
                            <th>Tổng thực bán</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
    </div>

    <script>
        $(function () {
            var now = new Date();
            var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
            var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
            $('#fromDate').val(formatDate(firstDay));
            $('#toDate').val(formatDate(lastDay));

            loadSummary();

            var table = $('#sourceByCustomerTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'SourceByCustomerApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.fromDate = $('#fromDate').val();
                        d.toDate = $('#toDate').val();
                        d.keyword = $('#sourceKeyword').val();
                    }
                },
                pageLength: 50,
                columns: [
                    { data: 'SourceName' },
                    { data: 'TotalBookings' },
                    { data: 'TotalGuests' },
                    { data: 'TotalAmount', render: function (data) {
                        return data ? parseFloat(data).toLocaleString('vi-VN') : '0';
                    }},
                    { data: 'TotalAmountThucBan', render: function (data) {
                        return data ? parseFloat(data).toLocaleString('vi-VN') : '0';
                    }}
                ]
            });

            $('#btnFilter').on('click', function () {
                loadSummary();
                table.ajax.reload();
            });
            $('#btnReset').on('click', function () {
                $('#fromDate').val(formatDate(firstDay));
                $('#toDate').val(formatDate(lastDay));
                $('#sourceKeyword').val('');
                loadSummary();
                table.ajax.reload();
            });
        });

        function loadSummary() {
            $.getJSON('SourceByCustomerSummaryApi.aspx', {
                fromDate: $('#fromDate').val(),
                toDate: $('#toDate').val(),
                keyword: $('#sourceKeyword').val()
            }, function (resp) {
                if (!resp || resp.error) return;
                $('#kpiSources').text(formatNumber(resp.TotalSources));
                $('#kpiBookings').text(formatNumber(resp.TotalBookings));
                $('#kpiGuests').text(formatNumber(resp.TotalGuests));
                $('#kpiRevenue').text(formatNumber(resp.TotalAmountThucBan));
            });
        }

        function formatNumber(val) {
            var num = parseFloat(val || 0);
            return num.toLocaleString('vi-VN');
        }

        function formatDate(d) {
            var mm = (d.getMonth() + 1).toString().padStart(2, '0');
            var dd = d.getDate().toString().padStart(2, '0');
            return d.getFullYear() + '-' + mm + '-' + dd;
        }
    </script>
</asp:Content>
