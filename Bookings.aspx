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
                <div class="row g-2 align-items-end">
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
                        <label class="form-label fw-semibold">Ngày đặt chỗ</label>
                        <asp:TextBox ID="txtBookingDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2 col-lg-2">
                        <label class="form-label fw-semibold">Nguồn</label>
                        <asp:TextBox ID="txtSource" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        
                    </div>
                    <div class="col-md-3">
                        
                    </div>
                    <div class="col-md-4 col-lg-4 d-flex align-items-end gap-2">
                        <asp:Button ID="btnFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="bookingsTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>TT</th>
                            <th>Booking</th>
                            <th>Kh&#225;ch</th>
                            <th>Kh&#225;ch h&#224;ng</th>
                            <th>Gi&#7899;i t&#237;nh</th>
                            <th>&#272;i&#7879;n tho&#7841;i</th>
                            <th>Qu&#7889;c gia</th>
                            <th>Thanh to&#225;n</th>
                            <th>Ng&#432;&#7901;i t&#7841;o</th>
                        </tr>
                    </thead>
                </table>
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
                        d.bookingDate = $('#<%=txtBookingDate.ClientID%>').val();
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
                    { data: 'OrderId' },
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
                $('#<%=txtBookingDate.ClientID%>').val('');
                $('#<%=txtSource.ClientID%>').val('');
                table.ajax.reload();
            });
        });
    </script>
</asp:Content>
