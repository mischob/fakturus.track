package com.fakturus.track.util

import android.os.Build
import android.view.HapticFeedbackConstants
import android.view.View

object HapticManager {
    fun timerStart(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.CONFIRM)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }

    fun timerStop(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.REJECT)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }

    fun timerPauseResume(view: View) {
        view.performHapticFeedback(HapticFeedbackConstants.CONTEXT_CLICK)
    }

    fun sessionFinished(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.CONFIRM)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }

    fun toggle(view: View) {
        view.performHapticFeedback(HapticFeedbackConstants.CLOCK_TICK)
    }

    fun error(view: View) {
        if (Build.VERSION.SDK_INT >= 34) {
            view.performHapticFeedback(HapticFeedbackConstants.REJECT)
        } else {
            view.performHapticFeedback(HapticFeedbackConstants.LONG_PRESS)
        }
    }
}
