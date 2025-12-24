<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Bookings.aspx.cs" Inherits="Bookings" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Danh sách đặt chỗ</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
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
                <div class="row g-3">
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Mã booking</label>
                        <asp:TextBox ID="txtOrderId" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Họ và tên khách</label>
                        <asp:TextBox ID="txtCustomerName" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Ngày đặt chỗ</label>
                        <asp:TextBox ID="txtBookingDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Nguồn</label>
                        <asp:TextBox ID="txtSource" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Sắp xếp</label>
                        <asp:DropDownList ID="ddlSort" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Mới nhất" Value="newest" Selected="True" />
                            <asp:ListItem Text="Giá trị booking (cao ↓ thấp)" Value="amount" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Số lượng/trang</label>
                        <asp:DropDownList ID="ddlPageSize" runat="server" CssClass="form-select">
                            <asp:ListItem Text="20" Value="20" Selected="True" />
                            <asp:ListItem Text="30" Value="30" />
                            <asp:ListItem Text="50" Value="50" />
                            <asp:ListItem Text="100" Value="100" />
                            <asp:ListItem Text="200" Value="200" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4 d-flex align-items-end gap-2">
                        <asp:Button ID="btnFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="bookingsTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Mã booking</th>
                            <th>Khách hàng</th>
                            <th>Giới tính</th>
                            <th>Điện thoại</th>
                            <th>Nguồn</th>
                            <th>Quốc gia</th>
                            <th>Người tạo</th>
                            <th>Tổng tiền</th>
                            <th>Thực bán</th>
                            <th>Hạn thanh toán</th>
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
                        d.sort = $('#<%=ddlSort.ClientID%>').val();
                    }
                },
                pageLength: parseInt($('#<%=ddlPageSize.ClientID%>').val(), 10),
                columns: [
                    { data: 'OrderId' },
                    { data: 'CustomerName' },
                    { data: 'Gender' },
                    { data: 'Phone' },
                    { data: 'Source' },
                    { data: 'ProductName', render: function (data) { return data || ''; } },
                    { data: 'CreatedBy' },
                    { data: 'Amount', render: renderMoney },
                    { data: 'AmountThucBan', render: renderMoney },
                    { data: 'DepositDeadline', render: renderDate }
                ]
            });
            $('#<%=ddlPageSize.ClientID%>').on('change', function () {
                var val = parseInt($(this).val(), 10) || 20;
                table.page.len(val).draw();
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
                $('#<%=ddlSort.ClientID%>').val('newest');
                $('#<%=ddlPageSize.ClientID%>').val('20').trigger('change');
                table.ajax.reload();
            });
        });
    </script>
</asp:Content>
