<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Bookings.aspx.cs" Inherits="Bookings" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Danh sách đặt chỗ</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .tag {
            display: inline-block;
            background: #eef2ff;
            color: #1d2353;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #dbe4ff;
            font-size: 12px;
        }
        .tag-phone {
            display: inline-block;
            background: #ecfdf3;
            color: #0f5132;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #c9f2dc;
            font-size: 12px;
        }
        #bookingsTable_length {
            display: none;
        }
        .tag-status {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
            border: 1px solid transparent;
            min-width: 32px;
            text-align: center;
        }
        .tag-status-OP {
            background: #e2e8f0;
            color: #1f2937;
            border-color: #cbd5e1;
        }
        .tag-status-CX {
            background: #fee2e2;
            color: #991b1b;
            border-color: #fecaca;
        }
        .tag-status-BK {
            background: #fef3c7;
            color: #92400e;
            border-color: #fde68a;
        }
        .tag-status-FP {
            background: #dcfce7;
            color: #166534;
            border-color: #bbf7d0;
        }
        .text-date {
            font-size: 12px;
        }
        #bookingsTable th,
        #bookingsTable td {
            white-space: nowrap;
        }
        #bookingsTable th:nth-child(8),
        #bookingsTable td:nth-child(8) {
            white-space: normal;
        }
        .tag-source {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            font-size: 12px;
            margin-top: 4px;
            border: 1px solid transparent;
        }
        .tag-source-default {
            background: #e9ecef;
            color: #374151;
            border-color: #dee2e6;
        }
        .tag-source-online {
            background: #e0f2fe;
            color: #075985;
            border-color: #bae6fd;
        }
        .tag-source-offline {
            background: #fef3c7;
            color: #92400e;
            border-color: #fde68a;
        }
        .tag-source-facebook {
            background: #dbeafe;
            color: #1d4ed8;
            border-color: #bfdbfe;
        }
        .tag-source-zalo {
            background: #e0e7ff;
            color: #3730a3;
            border-color: #c7d2fe;
        }
        .tag-tour {
            display: inline-block;
            background: #fff7ed;
            color: #9a3412;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 0;
            border: 1px solid #fed7aa;
            font-size: 12px;
        }
        #bookingDetailModal .modal-dialog {
            max-width: 900px;
        }
        #bookingDetailLoading {
            display: none !important;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-3">
                <div>
                    <h2 class="h4 mb-1 text-primary">Danh sách đặt chỗ</h2>
                    <div class="text-muted">Hiển thị các booking từ bảng order</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-2 align-items-end flex-lg-nowrap">
                    <div class="col-md-2 col-lg-1">
                        <label class="form-label fw-semibold">Mã booking</label>
                        <asp:TextBox ID="txtOrderId" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Họ và tên khách</label>
                        <asp:TextBox ID="txtCustomerName" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2 col-lg-1">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2 col-lg-2">
                        <label class="form-label fw-semibold">Từ ngày</label>
                        <asp:TextBox ID="txtFromDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2 col-lg-2">
                        <label class="form-label fw-semibold">Đến ngày</label>
                        <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2 col-lg-2">
                        <label class="form-label fw-semibold">Nguồn</label>
                        <asp:TextBox ID="txtSource" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3 d-flex align-items-end gap-2">
                        <asp:Button ID="btnFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                        <a id="btnExportExcel" runat="server" class="btn btn-success btn-sm" title="Export Excel" href="#">
                            <i class="bi bi-file-earmark-excel"></i>
                        </a>
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="bookingsTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>TT</th>
                            <th>Booking</th>
                            <th>Tour</th>
                            <th>Khách</th>
                            <th>Khách hàng</th>
                            <th>Giới tính</th>
                            <th>Điện thoại</th>
                            <th>Quốc gia</th>
                            <th>Thanh toán</th>
                            <th>Người tạo</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>

        <div class="modal fade" id="bookingDetailModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Chi tiết booking</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div id="bookingDetailLoading" class="d-flex justify-content-center my-3" style="display:none;">
                            <div class="spinner-border text-primary" role="status"></div>
                        </div>
                        <div id="bookingDetailContent" style="display:none;">
                            <div class="row mb-3">
                                <div class="col-md-4">
                                    <div class="fw-semibold">Booking</div>
                                    <div id="bookingDetailOrderId"></div>
                                </div>
                                <div class="col-md-4">
                                    <div class="fw-semibold">Tổng khách</div>
                                    <div id="bookingDetailTotalCustomers"></div>
                                </div>
                                <div class="col-md-4">
                                    <div class="fw-semibold">Tổng giá bán</div>
                                    <div id="bookingDetailTotalPrice"></div>
                                </div>
                            </div>
                            <div class="table-responsive">
                                <table class="table table-sm table-striped align-middle mb-0">
                                    <thead class="table-light">
                                        <tr>
                                            <th>Khách</th>
                                            <th>Giới tính</th>
                                            <th>Điện thoại</th>
                                            <th>Passport</th>
                                            <th>Phòng</th>
                                            <th>Giá bán</th>
                                        </tr>
                                    </thead>
                                    <tbody id="bookingDetailBody"></tbody>
                                </table>
                            </div>
                        </div>
                        <div id="bookingDetailError" class="text-danger" style="display:none;"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        function renderMoney(val) {
            if (val === null || val === undefined || val === '') return '';
            return parseFloat(val).toLocaleString('vi-VN');
        }
        function renderDate(val) {
            if (!val) return '';
            var d = new Date(val);
            if (isNaN(d.getTime())) return val;
            return d.toLocaleDateString('vi-VN');
        }
        function renderTags(val, cssClass) {
            if (!val) return '';
            var parts = String(val).split(/[;,]+/).map(function (p) { return p.trim(); }).filter(Boolean);
            if (!parts.length) return '';
            return parts.map(function (p) {
                return '<span class="' + cssClass + '">' + p + '</span>';
            }).join(' ');
        }
        $(function () {
            var table = $('#bookingsTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'BookingsApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.orderId = $('#<%=txtOrderId.ClientID%>').val();
                        d.customerName = $('#<%=txtCustomerName.ClientID%>').val();
                        d.phone = $('#<%=txtPhone.ClientID%>').val();
                        d.fromDate = $('#<%=txtFromDate.ClientID%>').val();
                        d.toDate = $('#<%=txtToDate.ClientID%>').val();
                        d.source = $('#<%=txtSource.ClientID%>').val();
                    }
                },
                pageLength: 50,
                columns: [
                    { data: 'Status', render: function (data) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<span class="tag-status tag-status-' + safe + '">' + safe + '</span>';
                    }},
                    { data: 'OrderId', render: function (data) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<a href="#" class="booking-link" data-orderid="' + safe + '">' + safe + '</a>';
                    } },
                    { data: null, render: function (data, type, row) {
                        var code = row && row.TourCode ? $('<div/>').text(row.TourCode).html() : '';
                        var date = row && row.DepartureDate ? $('<div/>').text(row.DepartureDate).html() : '';
                        if (!code && !date) return '';
                        var codeHtml = code ? '<div class="tag-tour">' + code + '</div>' : '';
                        var dateHtml = date ? '<div class="text-muted text-date">' + date + '</div>' : '';
                        return codeHtml + dateHtml;
                    } },
                    { data: 'CustomerCount' },
                    { data: null, render: function (data, type, row) {
                        var name = row && row.CustomerName ? $('<div/>').text(row.CustomerName).html() : '';
                        var source = row && row.Source ? $('<div/>').text(row.Source).html() : '';
                        if (!source) return name;
                        var key = String(row.Source || '').toLowerCase();
                        var cls = 'tag-source-default';
                        if (key.indexOf('online') >= 0) cls = 'tag-source-online';
                        else if (key.indexOf('offline') >= 0) cls = 'tag-source-offline';
                        else if (key.indexOf('facebook') >= 0) cls = 'tag-source-facebook';
                        else if (key.indexOf('zalo') >= 0) cls = 'tag-source-zalo';
                        return '<div>' + name + '</div><div class="tag-source ' + cls + '">' + source + '</div>';
                    }},
                    { data: 'Gender' },
                    { data: 'Phone', render: function (data) { return renderTags(data, 'tag-phone'); } },
                    { data: 'ProductName', render: function (data) { return renderTags(data, 'tag'); } },
                    { data: null, render: function (data, type, row) {
                        var amount = row && row.AmountThucBan ? renderMoney(row.AmountThucBan) : '';
                        var date = row && row.DepositDeadline ? renderDate(row.DepositDeadline) : '';
                        if (!amount && !date) return '';
                        return '<div>' + amount + '</div><div class="text-muted text-date">' + date + '</div>';
                    }},
                    { data: null, render: function (data, type, row) {
                        var name = row && row.CreatedBy ? $('<div/>').text(row.CreatedBy).html() : '';
                        var date = row && row.CreationDate ? $('<div/>').text(row.CreationDate).html() : '';
                        if (!name && !date) return '';
                        return '<div>' + name + '</div><div class="text-muted text-date">' + date + '</div>';
                    }}
                ]
            });
            table.on('page.dt', function () {
                $('html, body').animate({ scrollTop: 0 }, 200);
            });
            table.on('draw.dt', function () {
                $('html, body').animate({ scrollTop: 0 }, 200);
            });
            $('#<%=btnFilter.ClientID%>, #<%=btnReset.ClientID%>').attr('type', 'button');
            $('#<%=btnFilter.ClientID%>').on('click', function () { table.ajax.reload(); });
            $('#<%=btnReset.ClientID%>').on('click', function () {
                $('#<%=txtOrderId.ClientID%>').val('');
                $('#<%=txtCustomerName.ClientID%>').val('');
                $('#<%=txtPhone.ClientID%>').val('');
                $('#<%=txtFromDate.ClientID%>').val('');
                $('#<%=txtToDate.ClientID%>').val('');
                $('#<%=txtSource.ClientID%>').val('');
                table.ajax.reload();
            });

            $('#bookingsTable').on('click', '.booking-link', function (e) {
                e.preventDefault();
                var orderId = $(this).data('orderid');
                loadBookingDetail(orderId);
            });

            $('#<%=btnExportExcel.ClientID%>').on('click', function (e) {
                e.preventDefault();
                var url = 'BookingsExport.aspx?'
                    + 'orderId=' + encodeURIComponent($('#<%=txtOrderId.ClientID%>').val() || '')
                    + '&customerName=' + encodeURIComponent($('#<%=txtCustomerName.ClientID%>').val() || '')
                    + '&phone=' + encodeURIComponent($('#<%=txtPhone.ClientID%>').val() || '')
                    + '&fromDate=' + encodeURIComponent($('#<%=txtFromDate.ClientID%>').val() || '')
                    + '&toDate=' + encodeURIComponent($('#<%=txtToDate.ClientID%>').val() || '')
                    + '&source=' + encodeURIComponent($('#<%=txtSource.ClientID%>').val() || '');
                window.location.href = url;
            });
        });

        function loadBookingDetail(orderId) {
            var modalEl = document.getElementById('bookingDetailModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            $('#bookingDetailError').hide();
            $('#bookingDetailContent').hide();
            $('#bookingDetailBody').empty();
            $('#bookingDetailOrderId').text('');
            $('#bookingDetailTotalCustomers').text('');
            $('#bookingDetailTotalPrice').text('');
            $('#bookingDetailLoading').show();
            modal.show();
            $.ajax({
                url: 'BookingsDetailApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { orderId: orderId },
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#bookingDetailError').text(resp.error).show();
                        $('#bookingDetailLoading').hide();
                        return;
                    }
                    if (resp && resp.summary) {
                        $('#bookingDetailOrderId').text(resp.summary.OrderId || '');
                        $('#bookingDetailTotalCustomers').text(resp.summary.TotalCustomers || '0');
                        $('#bookingDetailTotalPrice').text(resp.summary.TotalPrice ? renderMoney(resp.summary.TotalPrice) : '0');
                    }
                    var rows = '';
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, o) {
                            rows += '<tr>'
                                + '<td>' + (o.CustomerName || '') + '</td>'
                                + '<td>' + (o.Gender || '') + '</td>'
                                + '<td>' + (o.Phone || '') + '</td>'
                                + '<td>' + (o.Passport || '') + '</td>'
                                + '<td>' + (o.Room || '') + '</td>'
                                + '<td>' + (o.Price ? renderMoney(o.Price) : '') + '</td>'
                                + '</tr>';
                        });
                    } else {
                        rows = '<tr><td colspan="6" class="text-center text-muted">Chưa có dữ liệu</td></tr>';
                    }
                    $('#bookingDetailBody').html(rows);
                    $('#bookingDetailLoading').hide();
                    $('#bookingDetailContent').show();
                },
                error: function () {
                    $('#bookingDetailError').text('Lỗi tải chi tiết booking').show();
                    $('#bookingDetailLoading').hide();
                }
            });
        }
    </script>
</asp:Content>
