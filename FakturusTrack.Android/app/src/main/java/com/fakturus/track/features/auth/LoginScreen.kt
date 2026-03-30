package com.fakturus.track.features.auth

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Laptop
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.fakturus.track.R
import com.fakturus.track.services.auth.AuthException
import com.fakturus.track.services.auth.AuthManager
import com.fakturus.track.services.auth.LoginProvider
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch

@Composable
fun LoginScreen(authManager: AuthManager) {
    val context = LocalContext.current
    val activity = context as ComponentActivity
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Icon(
            imageVector = Icons.Default.Schedule,
            contentDescription = null,
            modifier = Modifier.size(72.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(Modifier.height(16.dp))
        Text("Fakturus Track", style = MaterialTheme.typography.headlineLarge)
        Text(
            "Arbeitszeit erfassen. Einfach. Ueberall.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(Modifier.height(48.dp))

        LoginButton(stringResource(R.string.login_apple), Icons.Default.Phone, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.APPLE,
                onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton(stringResource(R.string.login_google), Icons.Default.Email, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.GOOGLE,
                onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton(stringResource(R.string.login_microsoft), Icons.Default.Laptop, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.MICROSOFT,
                onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton(stringResource(R.string.login_amazon), Icons.Default.ShoppingCart, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.AMAZON,
                onLoading = { isLoading = it }, onError = { errorMessage = it })
        }
        Spacer(Modifier.height(12.dp))
        LoginButton(stringResource(R.string.login_email), Icons.Default.Email, isLoading) {
            loginWith(scope, authManager, activity, LoginProvider.EMAIL,
                onLoading = { isLoading = it }, onError = { errorMessage = it })
        }

        if (isLoading) {
            Spacer(Modifier.height(16.dp))
            CircularProgressIndicator()
        }
        errorMessage?.let { error ->
            Spacer(Modifier.height(16.dp))
            Text(
                error,
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodySmall
            )
        }
    }
}

private fun loginWith(
    scope: CoroutineScope,
    authManager: AuthManager,
    activity: ComponentActivity,
    provider: LoginProvider,
    onLoading: (Boolean) -> Unit,
    onError: (String?) -> Unit
) {
    scope.launch {
        onLoading(true)
        onError(null)
        try {
            authManager.acquireTokenInteractively(activity, provider)
        } catch (_: AuthException.Cancelled) {
            // User cancelled, not an error
        } catch (_: Exception) {
            onError("Anmeldung fehlgeschlagen. Bitte versuchen Sie es erneut.")
        }
        onLoading(false)
    }
}

@Composable
private fun LoginButton(
    text: String,
    icon: ImageVector,
    isLoading: Boolean,
    onClick: () -> Unit
) {
    OutlinedButton(
        onClick = onClick,
        enabled = !isLoading,
        modifier = Modifier
            .fillMaxWidth()
            .height(50.dp)
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(20.dp))
        Spacer(Modifier.width(8.dp))
        Text(text)
    }
}
