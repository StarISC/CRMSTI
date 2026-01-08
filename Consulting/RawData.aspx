<%@ Page Language="C#" AutoEventWireup="true" CodeFile="RawData.aspx.cs" Inherits="Consulting_RawData" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Dữ liệu thô</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .form-section-title { font-weight: 700; color: #1d2353; }
        .form-hint { color: #64748b; }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <h4 class="form-section-title mb-2">Dữ liệu thô</h4>
            <p class="form-hint mb-4">Nhập thông tin để chuẩn bị lấy dữ liệu liên hệ.</p>

            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label fw-semibold">Tên tỉnh</label>
                    <asp:TextBox ID="txtProvince" runat="server" CssClass="form-control" placeholder="Ví dụ: TP. Hồ Chí Minh" />
                </div>
                <div class="col-md-8">
                    <label class="form-label fw-semibold">Đường link</label>
                    <asp:TextBox ID="txtSourceUrl" runat="server" CssClass="form-control" placeholder="https://..." />
                </div>
                <div class="col-md-2">
                    <label class="form-label fw-semibold">Trang từ</label>
                    <asp:TextBox ID="txtPageFrom" runat="server" CssClass="form-control" placeholder="1" />
                </div>
                <div class="col-md-2">
                    <label class="form-label fw-semibold">Trang đến</label>
                    <asp:TextBox ID="txtPageTo" runat="server" CssClass="form-control" placeholder="10" />
                </div>
                <div class="col-md-8 d-flex align-items-end gap-2">
                    <asp:Button ID="btnSubmit" runat="server" Text="Tải HTML" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
                    <asp:Button ID="btnClear" runat="server" Text="Xóa" CssClass="btn btn-outline-secondary" OnClick="btnClear_Click" />
                </div>
                <div class="col-12">
                    <asp:Label ID="lblStatus" runat="server" CssClass="text-muted small"></asp:Label>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm mt-3">
        <div class="card-body">
            <h6 class="form-section-title mb-3">Kết quả trích xuất</h6>
            <div class="table-responsive">
                <table class="table table-sm table-striped mb-0">
                    <thead>
                        <tr>
                            <th>Tên công ty</th>
                            <th>Mã số thuế</th>
                            <th>Người đại diện</th>
                            <th>Tên tỉnh</th>
                            <th>Điện thoại</th>
                            <th>Địa chỉ</th>
                            <th>Link chi tiết</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="rptResults" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td><%# Eval("CompanyName") %></td>
                                    <td class="text-nowrap"><%# Eval("TaxCode") %></td>
                                    <td><%# Eval("Representative") %></td>
                                    <td><%# Eval("ProvinceFromAddress") %></td>
                                    <td class="text-nowrap"><%# Eval("Phone") %></td>
                                    <td><%# Eval("Address") %></td>
                                    <td>
                                        <a href="<%# Eval("DetailUrl") %>" target="_blank">Xem</a>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</asp:Content>
