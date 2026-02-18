import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GiftService } from 'src/app/Services/gift-service';
import { AuthService } from 'src/app/Services/auth-service';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-report-winners-comp',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './report-winners-comp.html',
  styleUrl: './report-winners-comp.scss',
})
export class ReportWinnersComp implements OnInit {
  private giftSrv = inject(GiftService);
  public authSrv = inject(AuthService);

  winnersList: any[] = [];
  totalIncome: number = 0;

  arrDetailsByGiftId: any[] = []; 
  totalAmount: number = 0;       

  readonly IMAGE_BASE = 'https://localhost:44305/Images/'; 

  ngOnInit(): void {
    if (this.authSrv.isAdmin()) {
      this.loadReport();
      this.reportAchnasot();
    }
  }

  getAll() {
    this.loadReport();
  }

  loadReport(): void {
    this.giftSrv.reportWinners().subscribe({
      next: (data) => {
        this.winnersList = data.map((gift: any) => ({
          ...gift,
          safeImage: gift.giftImage ? this.IMAGE_BASE + gift.giftImage : 'assets/gift-placeholder.png'
        }));
        
        this.arrDetailsByGiftId = this.winnersList; 
      },
      error: (err) => console.error("שגיאה בטעינת דוח הזוכים:", err)
    });
  }

  reportAchnasot() {
    this.giftSrv.reportAchnasot().subscribe({
      next: (data) => {
        this.totalIncome = data;
        this.totalAmount = data; 
      },
      error: (err) => console.error("שגיאה בטעינת דוח ההכנסות:", err)
    });
  }

  ConfirmPurchase(id: number): void {
    if (this.authSrv.isAdmin()) {
      console.log("מנהל מאשר רכישה עבור מזהה:", id);
      alert("הפעולה בוצעה בהצלחה");
      this.loadReport();
    }
  }

  Delete(id: number): void {
    if (this.authSrv.isAdmin()) {
       console.log("מנהל מוחק רכישה עבור מזהה:", id);
    }
  }
}