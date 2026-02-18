import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from 'src/app/Services/auth-service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-login-comp',
  standalone: true, 
  imports: [
    
    MatSnackBarModule,
    ReactiveFormsModule, 
    CommonModule, 
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    
  ], 
  templateUrl: './login-comp.html',
  styleUrl: './login-comp.scss',
})
export class LoginComp implements OnInit {
  private fb = inject(FormBuilder);
  public authSrv = inject(AuthService); 
  private router = inject(Router);
  
  loginForm!: FormGroup;

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      UserName: ['', [Validators.required]],
      Password: ['', [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      const { UserName, Password } = this.loginForm.value;
      this.login(UserName, Password);
    } else {
      this.loginForm.markAllAsTouched();
    }
  }

  login(userName: string, password: string) {
  this.authSrv.Login(userName, password).subscribe({
    next: (data: any) => {
      if (data && data.token) {
        localStorage.setItem('token', data.token);
        localStorage.setItem('userRole', data.role || 'user');
        localStorage.setItem('userName', data.userName || userName);

        alert(`שלום ${data.userName || userName}, התחברת בהצלחה!`);
        this.router.navigate(['/home']); 
      } else {
        alert("התחברות הצליחה אך חסרים נתוני גישה. פני למנהל המערכת.");
      }
    },
    error: (err) => {
      const serverMessage = err.error?.message || "אירעה שגיאה בהתחברות. נסו שוב מאוחר יותר.";
      alert(serverMessage); 
      console.error("Login Error Details:", err);
    }
  });
}
}