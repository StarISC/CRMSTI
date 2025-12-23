<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Dashboard</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row g-3">
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <div class="d-flex align-items-center mb-2">
                        <i class="bi bi-speedometer2 text-primary me-2 fs-4"></i>
                        <h5 class="card-title mb-0">Dashboard</h5>
                    </div>
                    <p class="text-muted mb-3">Tổng quan nhanh: chọn màn để bắt đầu.</p>
                    <div class="d-grid gap-2">
                        <a class="btn btn-primary" href="Bookings.aspx">Danh sách đặt chỗ</a>
                        <a class="btn btn-outline-primary" href="Customers.aspx">Danh sách khách hàng</a>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <div class="d-flex align-items-center mb-2">
                        <i class="bi bi-people text-success me-2 fs-4"></i>
                        <h5 class="card-title mb-0">Khách hàng</h5>
                    </div>
                    <p class="text-muted mb-3">Theo dõi khách hàng cần chăm sóc và lịch sử đặt chỗ.</p>
                    <ul class="list-unstyled mb-0">
                        <li class="mb-2"><i class="bi bi-dot"></i>Danh sách khách hàng</li>
                        <li class="mb-2"><i class="bi bi-dot"></i>Danh sách đặt chỗ</li>
                        <li class="mb-2"><i class="bi bi-dot"></i>Đang tư vấn</li>
                    </ul>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <div class="d-flex align-items-center mb-2">
                        <i class="bi bi-info-circle text-warning me-2 fs-4"></i>
                        <h5 class="card-title mb-0">Ghi chú</h5>
                    </div>
                    <p class="text-muted mb-3">Dashboard chi tiết sẽ bổ sung sau khi hoàn thiện yêu cầu dữ liệu.</p>
                    <p class="mb-0 text-muted small">Bạn có thể chuyển sang menu phía trên để xem danh sách đặt chỗ.</p>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
