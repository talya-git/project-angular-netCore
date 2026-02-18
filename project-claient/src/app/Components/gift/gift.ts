import { Component, inject, Input, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { giftModel } from '../../Models/GiftModel';
import { GiftService } from '../../Services/gift-service';
import { AuthService } from 'src/app/Services/auth-service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router'; 
@Component({
  selector: 'app-gift',
  standalone: true,
  imports: [CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule],
  templateUrl: './gift.html',
  styleUrl: './gift.scss',
})
export class Gift implements OnChanges {
  
// gift.ts
frmGift: FormGroup = new FormGroup({
  id: new FormControl(0),
  name: new FormControl('', [Validators.required, Validators.minLength(2)]),
  priceCard: new FormControl(0, [Validators.required]),
  donorId: new FormControl(0, [Validators.required, Validators.min(1)]),
  donorName: new FormControl(''),
  Category: new FormControl(''),
  GiftImage: new FormControl('')
});
   public authSrv: AuthService = inject(AuthService); 
   private router: Router = inject(Router);
  
  giftSrv: GiftService = inject(GiftService);
showAddForm:Boolean=true;
  @Input()
  id: number = -1;

  ngOnChanges(c: SimpleChanges) {
    console.log("Is Admin Check:", this.authSrv.isAdmin());
  console.log("Role in Storage:", localStorage.getItem('userRole'));
    if (c['id'] && this.id > 0) {
      this.showAddForm = true; 
      this.giftSrv.getById(this.id).subscribe((giftFromServer) => {
        if (giftFromServer) {
          this.frmGift.patchValue(giftFromServer);
        }
      });
    } 
    else if (c['id'] && (this.id === -1 || this.id === 0))
       {
      this.frmGift.reset({ 
        id: 0, name: '', priceCard: 0, donorId: '',donorName:'',Category:'',GiftImage:''
       });
    }
  }

  addGift() {
  if (!this.authSrv.isAdmin()) {
    alert('אינך מורשה לבצע פעולה זו!');
    return;
  }

  const gift: giftModel = this.frmGift.value;

  if (this.id <= 0) {
    this.giftSrv.add(gift).subscribe({
      next: (res: any) => {
        alert(res.message || 'המתנה נוספה בהצלחה!');
        this.afterSaveActions();
      },
      error: (err) => {
        const msg = err.error?.message || 'שגיאה בהוספת המתנה';
        alert(msg);
        console.error("Add Gift Error:", err);
      }
    });
  } else {
    this.giftSrv.update(gift).subscribe({
      next: (res: any) => {
        alert(res.message || 'המתנה עודכנה בהצלחה!');
        this.afterSaveActions();
      },
      error: (err) => {
        const msg = err.error?.message || 'שגיאה בעדכון המתנה';
        alert(msg);
        console.error("Update Gift Error:", err);
      }
    });
  }
}
closeEdit() {
  this.showAddForm = false;
  this.id = -1; 
  this.frmGift.reset({ id: 0 });
}
private afterSaveActions() {
  this.giftSrv.refreshList$.next(); 
  this.id = -1;
  this.showAddForm = false;
  this.frmGift.reset({ id: 0 });
  this.router.navigate(['/home']);
}
}