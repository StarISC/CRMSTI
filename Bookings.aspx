<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Bookings.aspx.cs" Inherits="Bookings" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Danh s&#225;ch &#273;&#7863;t ch&#7895;</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-3">
                <div>
                    <h2 class="h4 mb-1 text-primary">Danh s&#225;ch &#273;&#7863;t ch&#7895;</h2>
                    <div class="text-muted">Hi&#7875;n th&#7883; c&#225;c booking t&#7915; b&#7843;ng order</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-3">
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">M&#227; booking</label>
                        <asp:TextBox ID="txtOrderId" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">H&#7885; v&#224; t&#234;n kh&#225;ch</label>
                        <asp:TextBox ID="txtCustomerName" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">&#272;i&#7879;n tho&#7841;i</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Ng&#224;y &#273;&#7863;t ch&#7895;</label>
                        <asp:TextBox ID="txtBookingDate" runat="server" CssClass="form-control" TextMode="Date" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Ngu&#7891;n</label>
                        <asp:TextBox ID="txtSource" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">S&#7855;p x&#7871;p</label>
                        <asp:DropDownList ID="ddlSort" runat="server" CssClass="form-select">
                            <asp:ListItem Text="M&#7899;i nh&#7845;t" Value="newest" Selected="True" />
                            <asp:ListItem Text="Gi&#225; tr&#7883; booking (cao &#8595; th&#7845;p)" Value="amount" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">S&#7889; l&#432;&#7907;ng/trang</label>
                        <asp:DropDownList ID="ddlPageSize" runat="server" CssClass="form-select">
                            <asp:ListItem Text="20" Value="20" Selected="True" />
                            <asp:ListItem Text="30" Value="30" />
                            <asp:ListItem Text="50" Value="50" />
                            <asp:ListItem Text="100" Value="100" />
                            <asp:ListItem Text="200" Value="200" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4 d-flex align-items-end gap-2">
                        <asp:Button ID="btnFilter" runat="server" Text="L&#7885;c" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="X&#243;a l&#7885;c" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="bookingsTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>M&#227; booking</th>
                            <th>Kh&#225;ch h&#224;ng</th>
                            <th>Gi&#7899;i t&#237;nh</th>
                            <th>&#272;i&#7879;n tho&#7841;i</th>
                            <th>Ngu&#7891;n</th>
                            <th>Qu&#7889;c gia</th>
                            <th>Ng&#432;&#7901;i t&#7841;o</th>
                            <th>T&#7893;ng ti&#7873;n</th>
                            <th>Th&#7921;c b&#225;n</th>
                            <th>H&#7841;n thanh to&#225;n</th>
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
