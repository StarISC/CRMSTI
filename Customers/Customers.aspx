<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Customers.aspx.cs" Inherits="Customers" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Danh sách khách hàng</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    
    <style>
        .gv-pager {
            background: linear-gradient(90deg, #e8edff, #f5f7ff);
            text-align: center;
            padding: 8px 0;
        }
        th {
            white-space: nowrap;
        }
        .tag {
            display: inline-block;
            background: #eef2ff;
            color: #1d2353;
            padding: 4px 10px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #dbe4ff;
            font-size: 13px;
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
        .tag-code {
            display: inline-block;
            background: #fff3cd;
            color: #8a4b0f;
            padding: 4px 10px;
            border-radius: 999px;
            font-weight: 700;
            margin-top: 4px;
            border: 1px solid #ffe4a3;
            font-size: 12px;
            white-space: nowrap;
        }
        .loading-overlay {
            position: fixed;
            inset: 0;
            background: rgba(255, 255, 255, 0.6);
            display: none;
            align-items: center;
            justify-content: center;
            z-index: 1050;
        }
        .nowrap {
            white-space: nowrap;
        }
        .phone-link {
            color: #1f4fbf;
            font-weight: 600;
            text-decoration: none;
            letter-spacing: 0.2px;
        }
        .phone-link:hover,
        .phone-link:focus {
            color: #173a8c;
            text-decoration: underline;
        }
        #customersTable td:first-child {
            font-variant-numeric: tabular-nums;
        }
        #customersTable_length_custom {
            display: none;
        }
        #customerDetailModal .modal-dialog {
            width: 99%;
            max-width: 900px;
            margin-left: auto;
            margin-right: auto;
        }
        @media (max-width: 768px) {
            #customerDetailModal .modal-dialog {
                width: 99%;
                max-width: 99%;
            }
        }
        #detailLoading {
            display: none !important;
        }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-3">
                <div>
                    <h2 class="h4 mb-1 text-primary">Danh sách khách hàng</h2>
                    <div class="text-muted">Khách đã mua tour, nhóm theo điện thoại</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-3 align-items-end flex-nowrap">
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Họ và tên khách</label>
                        <asp:TextBox ID="txtCustomerName" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Từ ngày</label>
                        <asp:TextBox ID="txtFromDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Đến ngày</label>
                        <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-3 d-flex gap-2 justify-content-end flex-nowrap">
                        <asp:Button ID="btnFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                        <asp:Button ID="btnExport" runat="server" Text="Export Excel" CssClass="btn btn-success" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="d-flex justify-content-between align-items-center mb-2">
                <div></div>
                <div id="customersTable_length_custom"></div>
            </div>
            <div class="table-responsive">
                <table id="customersTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Điện thoại</th>
                            <th>Khách hàng</th>
                            <th>Giới tính</th>
                                        <th>Qu&#7889;c gia</th>
                            <th>Đã mua</th>
                            <th>Tour gần nhất</th>
                            <th>Tổng chi</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
    </div>
    <div id="customersLoading" class="loading-overlay">
        <div class="spinner-border text-primary" role="status" aria-hidden="true"></div>
    </div>
    <div class="modal fade" id="customerDetailModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
                                <div class="modal-header">
                    <h5 class="modal-title">Chi tiết khách hàng</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div id="detailLoading" class="d-flex justify-content-center my-3">
                        <div class="spinner-border text-primary" role="status"></div>
                    </div>
                    <div id="detailContent" style="display:none;">
                        <div class="row mb-3">
                            <div class="col-md-4">
                                <div class="fw-semibold">Khách hàng:</div>
                                <div id="detailName"></div>
                            </div>
                            <div class="col-md-4">
                                <div class="fw-semibold">Điện thoại</div>
                                <div id="detailPhone"></div>
                            </div>
                            <div class="col-md-4">
                                <div class="fw-semibold">Giới tính</div>
                                <div id="detailGender"></div>
                            </div>
                        </div>
                        <div class="row mb-3">
                            <div class="col-md-6">
                                <div class="fw-semibold">Đã mua</div>
                                <div id="detailTotalBookings"></div>
                            </div>
                            <div class="col-md-6">
                                <div class="fw-semibold">Tổng chi</div>
                                <div id="detailTotalAmount"></div>
                            </div>
                        </div>
                        <div class="mb-3">
                            <div class="fw-semibold">Quốc gia</div>
                            <div id="detailCountries"></div>
                        </div>
                        <div class="mb-2 fw-bold text-primary border-bottom border-2 pb-1" style="font-size: 1.15rem; letter-spacing: 0.5px;">L&#7883;ch s&#7917; &#273;&#7863;t ch&#7895;</div>
                        <div class="table-responsive">
                            <table class="table table-sm table-striped align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th>TT</th>
                                        <th>Booking</th>
                                        <th>Tour</th>
                                        <th>Kh&#225;ch</th>
                                        <th>Th&#7921;c b&#225;n</th>
                                        <th>Qu&#7889;c gia</th>
                                        <th>Ngu&#7891;n</th>
                                        <th>#</th>
                                    </tr>
                                </thead>
                                <tbody id="detailOrdersBody"></tbody>
                            </table>
                        </div>
                    </div>
                    <div id="detailError" class="text-danger" style="display:none;"></div>
                </div>
                <div class="modal-body">
                    <div id="detailLoading" class="d-flex justify-content-center my-3">
                        <div class="spinner-border text-primary" role="status"></div>
                    </div>
                    <div id="detailContent" style="display:none;">
                        <div class="mb-3">
                            <div class="fw-semibold">Khách hàng:</div>
                            <div id="detailName"></div>
                        </div>
                        <div class="row mb-3">
                            <div class="col-md-4">
                                <div class="fw-semibold">Điện thoại</div>
                                <div id="detailPhone"></div>
                            </div>
                            <div class="col-md-4">
                                <div class="fw-semibold">Giới tính</div>
                                <div id="detailGender"></div>
                            </div>
                            <div class="col-md-4">
                                <div class="fw-semibold">Lần mua</div>
                                <div id="detailTotalBookings"></div>
                            </div>
                        </div>
                        <div class="mb-3">
                            <div class="fw-semibold">Tổng chi</div>
                            <div id="detailTotalAmount"></div>
                        </div>
                        <div class="mb-3">
                            <div class="fw-semibold">Quốc gia</div>
                            <div id="detailCountries"></div>
                        </div>
                        <div class="mb-2 fw-bold text-primary border-bottom border-2 pb-1" style="font-size: 1.15rem; letter-spacing: 0.5px;">L&#7883;ch s&#7917; &#273;&#7863;t ch&#7895;</div>
                        <div class="table-responsive">
                            <table class="table table-sm table-striped align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th>TT</th>
                                        <th>Booking</th>
                                        <th>Tour</th>
                                        <th>Kh&#225;ch</th>
                                        <th>Th&#7921;c b&#225;n</th>
                                        <th>Qu&#7889;c gia</th>
                                        <th>Ngu&#7891;n</th>
                                        <th>#</th>
                                    </tr>
                                </thead>
                                <tbody id="detailOrdersBody"></tbody>
                            </table>
                        </div>
                    </div>
                    <div id="detailError" class="text-danger" style="display:none;"></div>
                </div>
            </div>
        </div>
    </div>
    <script>
        function renderTags(raw) {
            if (!raw) return '';
            return raw.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                .map(function (x) { return '<span class="tag">' + $('<div/>').text(x).html() + '</span>'; }).join('');
        }
        function reloadCustomers() {
            $('#customersTable').DataTable().ajax.reload();
            return false;
        }
        $(function () {
            var table = $('#customersTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'CustomersApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.phone = $('#<%=txtPhone.ClientID%>').val();
                        d.customerName = $('#<%=txtCustomerName.ClientID%>').val();
                        d.fromDate = $('#<%=txtFromDate.ClientID%>').val();
                        d.toDate = $('#<%=txtToDate.ClientID%>').val();
                    },
                    dataSrc: function (json) {
                        if (json.error) {
                            console.error('CustomersApi error:', json.error);
                            alert('Lỗi tải dữ liệu: ' + json.error);
                            return [];
                        }
                        return json.data;
                    }
                },
                pageLength: 50,
                lengthMenu: [[20, 30, 50, 100, 200], [20, 30, 50, 100, 200]],
                columns: [
                    { data: 'Phone', className: 'nowrap', render: function (data, type, row) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<a href="#" class="phone-link" data-phone="' + safe + '">' + safe + '</a>';
                    }},
                    { data: 'CustomerName', className: 'nowrap' },
                    { data: 'Gender' },
                    { data: 'ProductName', render: function (data) { return renderTags(data); }, orderable: false },
                    { data: 'TotalBookings' },
                    { data: null, render: function (data, type, row) {
                        if (!row || (!row.LatestDeparture && !row.LatestCode)) return '';
                        var dateLine = row.LatestDeparture ? '<div>' + $('<div/>').text(row.LatestDeparture).html() + '</div>' : '';
                        var codeLine = row.LatestCode ? '<div class="tag-code">' + $('<div/>').text(row.LatestCode).html() + '</div>' : '';
                        return dateLine + codeLine;
                    } },
                    { data: 'TotalAmountThucBan', render: function (data) { return data ? parseFloat(data).toLocaleString('vi-VN') : ''; } }
                ]
            });
            $('#customersTable_length_custom').empty().append($('#customersTable_length'));
            table.on('page.dt', function () {
                $('html, body').animate({ scrollTop: 0 }, 200);
            });
            table.on('draw.dt', function () {
                $('html, body').animate({ scrollTop: 0 }, 200);
            });
            table.on('processing.dt', function (e, settings, processing) {
                $('#customersLoading').css('display', processing ? 'flex' : 'none');
            });
            $('#<%=btnFilter.ClientID%>, #<%=btnReset.ClientID%>').attr('type', 'button');
            $('#<%=btnFilter.ClientID%>').on('click', function () { table.ajax.reload(); });
            $('#<%=btnReset.ClientID%>').on('click', function () {
                $('#<%=txtPhone.ClientID%>').val('');
                $('#<%=txtCustomerName.ClientID%>').val('');
                $('#<%=txtFromDate.ClientID%>').val('');
                $('#<%=txtToDate.ClientID%>').val('');
                table.ajax.reload();
            });
            var $exportBtn = $('#<%=btnExport.ClientID%>');
            if ($exportBtn.length) {
                $exportBtn.attr('type', 'button');
                $exportBtn.on('click', function () {
                    var phone = encodeURIComponent($('#<%=txtPhone.ClientID%>').val() || '');
                    var name = encodeURIComponent($('#<%=txtCustomerName.ClientID%>').val() || '');
                    var fromDate = encodeURIComponent($('#<%=txtFromDate.ClientID%>').val() || '');
                    var toDate = encodeURIComponent($('#<%=txtToDate.ClientID%>').val() || '');
                    window.open('CustomersExport.aspx?phone=' + phone + '&customerName=' + name + '&fromDate=' + fromDate + '&toDate=' + toDate, '_blank');
                });
            }

            $('#customersTable').on('click', '.phone-link', function (e) {
                e.preventDefault();
                var phone = $(this).data('phone');
                loadCustomerDetail(phone);
            });
        });

                function loadCustomerDetail(phone) {
            var modalEl = document.getElementById('customerDetailModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            $('#detailError').hide();
            $('#detailContent').hide();
            $('#detailOrdersBody').empty();
            $('#detailLoading').show();
            modal.show();
            $.ajax({
                url: 'CustomersDetailApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { phone: phone },
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#detailError').text(resp.error).show();
                        return;
                    }
                    $('#detailContent').show();
                    $('#detailName').text(resp.CustomerName || '');
                    $('#detailPhone').text(resp.Phone || '');
                    $('#detailGender').text(resp.Gender || '');
                    $('#detailTotalBookings').text(resp.TotalBookings || '0');
                    $('#detailTotalAmount').text(resp.TotalAmountThucBan ? parseFloat(resp.TotalAmountThucBan).toLocaleString('vi-VN') : '0');
                    $('#detailCountries').html(renderTags(resp.ProductName));
                    var rows = '';
                    if (resp.Orders && resp.Orders.length) {

                        $.each(resp.Orders, function (i, o) {
                            var status = o.Status || "";
                            var tag = status ? '<span class="tag-status tag-status-' + status + '">' + $('<div/>').text(status).html() + '</span>' : "";
                            var info = (o.CreationDate || "") + '<br><span class="text-muted">' + (o.CreatedBy || "") + '</span>';



















                            var info = (o.CreationDate || '') + '<br><span class="text-muted">' + (o.CreatedBy || '') + '</span>';
                            rows += '<tr>'
                                + '<td>' + tag + '</td>'
                                + '<td>' + (o.OrderId || '') + '</td>'
                                + '<td>' + (o.ProductCode || '') + '</td>'
                                + '<td>' + (o.NumGuests || '') + '</td>'
                                + '<td>' + (o.AmountThucBan ? parseFloat(o.AmountThucBan).toLocaleString('vi-VN') : '') + '</td>'
                                + '<td>' + (renderTags(o.Countries) || '') + '</td>'
                                + '<td>' + (o.Source || '') + '</td>'
                                + '<td>' + info + '</td>'
                                + '</tr>';
                        });
                    } else {
                        rows = '<tr><td colspan="8" class="text-center text-muted">Chưa có booking</td></tr>';
                    }
                    $('#detailOrdersBody').html(rows);
                },
                error: function () {
                    $('#detailError').text('Lỗi tại Chi tiết khách hàng').show();
                },
                complete: function () {
                    $('#detailLoading').hide();
                }
            });
        }</script>
</asp:Content>









