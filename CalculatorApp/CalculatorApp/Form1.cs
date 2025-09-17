using System;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculatorApp
{
	/// <summary>
	/// アプリ全体で使用する定数をカテゴリごとに整理
	/// </summary>
	internal static class Constants
	{
		/// <summary>フォント関連の定数</summary>
		internal static class FontSize
		{
			/// <summary>エラーメッセージフォントサイズ</summary>
			internal const float ERROR_MESSAGE = 20.0f;

			/// <summary>計算結果表示欄の基準フォントサイズ</summary>
			internal const float RESULT_DISPLAY_BASE = 36f;

			/// <summary>途中計算結果欄の基準フォントサイズ</summary>
			internal const float EXPRESSION_DISPLAY_BASE = 10f;

			/// <summary>フォントの下限サイズ</summary>
			internal const float MIN_LIMIT = 14f;

			/// <summary>フォントの縮小幅</summary>
			internal const float REDUCTION_STEP = 0.5f;

			/// <summary>フォント更新時の差分許容（EPS）</summary>
			internal const float SIZE_EPSILON = 0.1f;
		}

		/// <summary>演算子記号の定数</summary>
		internal static class Symbol
		{
			/// <summary>加算</summary>
			internal const string ADD = "+";

			/// <summary>減算</summary>
			internal const string SUBTRACT = "-";

			/// <summary>乗算</summary>
			internal const string MULTIPLY = "×"; // ボタン表記の都合で × も許可

			/// <summary>除算</summary>
			internal const string DIVIDE = "÷";   // 同上 ÷ も許可

			/// <summary>等号</summary>
			internal const string EQUAL = "=";

		}

		/// <summary>数値や表示桁に関する定数</summary>
		internal static class Numeric
		{
			/// <summary>初期値 0</summary>
			internal const decimal INITIAL_VALUE = 0m;

			/// <summary>表示用ゼロ</summary>
			public const string ZERO_VALUE = "0";

			/// <summary>％→小数 乗数</summary>
			internal const decimal PERCENT_MULTIPLY = 0.01m;

			/// <summary>整数部の最大表示桁数</summary>
			internal const int MAX_INTEGER_DISPLAY_DIGITS = 16;

			/// <summary>0.から始まる場合の最大表示桁数</summary>
			internal const int MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO = 17;

			public const int MAX_TOTAL_FRACTION_DIGITS = 15; // ← 追加

			public const int MAX_SIGNIFICANT_DIGITS = 17; // ← NEW!
		}

		/// <summary>エラーメッセージ</summary>
		internal static class ErrorMessage
		{
			internal const string OVERFLOW = "計算範囲を超えました";
			internal const string DIVIDE_BY_ZERO = "0で割ることはできません";
			internal const string UNDEFINED = "結果が定義されていません";
		}

		/// <summary>特殊表示</summary>
		internal static class SpecialDisplay
		{
			internal const string NEGATE_FUNCTION = "negate";
		}
	}

	public partial class Form1 : Form
	{
		/// <summary>最初の値</summary>
		private decimal FirstValue = 0m;

		/// <summary>2番目の値</summary>
		private decimal SecondValue = 0m;

		/// <summary>結果欄の上書き入力フラグ</summary>
		private bool TextOverwrite = false;

		/// <summary>小数点入力済みフラグ</summary>
		private bool NumDot = false;

		/// <summary>エラー状態</summary>
		private bool IsErrorState = false;

		/// <summary>現在の入力を CE でクリアしたか</summary>
		private bool IsClearEntry = false;

		/// <summary>内部の現在表示値（表示文字列と分離）</summary>
		private decimal DisplayValue = Constants.Numeric.INITIAL_VALUE;

		/// <summary>±押下時にフォーマット保持するか</summary>
		private bool PreserveFormatOnToggle = false;

		/// <summary>直近のユーザー生入力（カンマなし）</summary>
		private string lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;

		/// <summary>直前が％か</summary>
		private bool lastActionWasPercent = false;

		/// <summary>＝直後に途中式を消したか</summary>
		private bool ClearedExprAfterEqual = false;

		/// <summary>基準フォントサイズ（初期値）</summary>
		private float defaultFontSize;

		/// <summary>途中式欄の基準フォントサイズ（初期値）</summary>
		private float defaultExpressionFontSize;

		/// <summary>±直近押下</summary>
		private bool isNegated = false;

		/// <summary>エラー時に無効化するボタン（＝は含めない）</summary>
		private Button[] DisabledButtonsOnError;

		/// <summary>
		/// 演算子の種類を定義する列挙型
		/// </summary>
		private enum OperatorType
		{
			/// <summary>演算なし</summary>
			NON,
			/// <summary>加算</summary>
			ADD,
			/// <summary>減算</summary>
			SUBTRACT,
			/// <summary>乗算</summary>
			MULTIPLY,
			/// <summary>除算</summary>
			DIVIDE,
			/// <summary>パーセント</summary>
			PERCENT
		}

		/// <summary>現在の演算子種別を保持する変数</summary>
		private OperatorType currentOperatorType = OperatorType.NON;

		/// <summary>
		/// フォームのコンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();

			// ディスプレイサイズ固定化
			this.MaximumSize = this.Size;
			this.MinimumSize = this.Size;
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;

			// エラー時に無効化するボタン
			DisabledButtonsOnError = new Button[]
            {
                btnDot, btnTogglesign, btnPercent, btnPlus,
                btnMinus, btnMultiply, btnDivide, btnEnter
            };
		}

		/// <summary>
		/// フォームの初期化処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			textResult.Text = Constants.Numeric.ZERO_VALUE;
			TextOverwrite = true;

			textResult.Font = new Font(textResult.Font.FontFamily, Constants.FontSize.RESULT_DISPLAY_BASE, textResult.Font.Style);
			textExpression.Font = new Font(textExpression.Font.FontFamily, Constants.FontSize.EXPRESSION_DISPLAY_BASE, textExpression.Font.Style);

			// 基準サイズの保持
			defaultFontSize = textResult.Font.Size;
			defaultExpressionFontSize = textExpression.Font.Size;

			// 計算結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
			textResult.ReadOnly = true;
			textResult.TextAlign = HorizontalAlignment.Right;
			textResult.BorderStyle = BorderStyle.None;

			// 途中結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
			textExpression.ReadOnly = true;
			textExpression.TextAlign = HorizontalAlignment.Right;
			textExpression.BorderStyle = BorderStyle.None;
		}


		/// <summary>
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void textResult_TextChanged(object sender, EventArgs e)
		{
			AutoFitResultFont();
		}

		/// <summary>
		/// 
		/// </summary>
		private void textExpression_TextChanged(object sender, EventArgs e)
		{


		}

		/// <summary>
		/// 数字ボタン押下時のイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnNum_Click(object sender, EventArgs e)
		{
			var btn = sender as Button;
			if (btn == null)
			{
				return;
			}
			if (IsError())
			{
				ResetCalculatorState();
			}
			OnDigitButton(btn.Text);
		}

		/// <summary>
		/// 小数点キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnDot_Click(object sender, EventArgs e)
		{
			HandleInitialState();
			OnDotButton();
		}

		/// <summary>
		/// 演算子キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnOperation_Click(object sender, EventArgs e)
		{
			var btn = sender as Button;
			if (btn == null) return;

			var op = OperatorType.NON;

			switch (btn.Text)
			{
				case Constants.Symbol.ADD:
					op = OperatorType.ADD;
					break;

				case Constants.Symbol.SUBTRACT:
					op = OperatorType.SUBTRACT;
					break;

				case Constants.Symbol.MULTIPLY:
					op = OperatorType.MULTIPLY;
					break;

				case Constants.Symbol.DIVIDE:
					op = OperatorType.DIVIDE;
					break;
			}

			OnOperatorButton(op);
		}


		/// <summary>
		/// イコールキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnEnter_Click(object sender, EventArgs e)
		{
			OnEqualsButton();
		}

		/// <summary>
		/// ％キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnPercent_Click(object sender, EventArgs e)
		{
			OnPercentButton();
		}

		/// <summary>
		/// クリアエントリーキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClearEntry_Click(object sender, EventArgs e)
		{
			OnClearEntryButton();
		}

		/// <summary>
		/// クリアキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClear_Click(object sender, EventArgs e)
		{
			OnClearButton();
		}

		/// <summary>
		/// 桁下げキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnBack_Click(object sender, EventArgs e)
		{
			OnBackspaceButton();
		}

		/// <summary>
		///サインチェンジキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTogglesign_Click(object sender, EventArgs e)
		{
			OnToggleSignButton();
		}

		/// <summary>
		/// 最前面表示キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTopMost_Click(object sender, EventArgs e)
		{
			this.TopMost = !this.TopMost;
		}

		/// <summary>
		/// 数字キー入力のメイン処理。
		/// ユーザーが押した数字を、現在の電卓の状態に応じて処理
		/// </summary>
		/// <param name="digit">入力された数字を表す文字列</param>
		private void OnDigitButton(string digit)
		{
			HandleInitialState();
			SetButtonsEnabled(true);
			lastActionWasPercent = false;

			if (IsExponentDisplay())
			{
				TextOverwrite = true;
				NumDot = false;
			}

			var current = textResult.Text.Replace(",", "");
			if (!IsInputValid(current, digit))
			{
				return;
			}

			if (TextOverwrite)
			{
				StartNewNumber(digit);
			}
			else
			{
				AppendDigit(digit);
			}

			UpdateTextResultWithCommas();
			IsClearEntry = false;

			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 小数点キー入力のメイン処理。
		/// 小数点の重複入力を防ぎ、入力状態に応じて表示を更新
		/// </summary>
		private void OnDotButton()
		{
			HandleInitialState();
			lastActionWasPercent = false;

			if (NumDot)
			{
				return;
			}

			if (TextOverwrite)
			{
				textResult.Text = "0.";
				TextOverwrite = false;
			}
			else
			{
				textResult.Text += ".";
			}
			NumDot = true;

			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 演算子キー入力のメイン処理。
		/// 現在の状態（直前の操作、入力値、保留中の計算）を考慮し、
		/// 適切な計算を実行して、結果と表示を更新
		/// </summary>
		/// <param name="op">入力された演算子タイプ</param>
		private void OnOperatorButton(OperatorType op)
		{
			if (IsError())
			{
				ResetCalculatorState();
				return;
			}

			try
			{
				if (lastActionWasPercent && currentOperatorType != OperatorType.NON && !ExpressionEndsWithEqual())
				{
					var cur = GetCurrentValue();
					PerformPendingCalculation(cur);
					if (IsError())
					{
						return;
					}

					DisplayNumber(FirstValue, true);
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);

					lastActionWasPercent = false;
					TextOverwrite = true;
					NumDot = false;
					return;
				}

				if (IsClearEntry)
				{
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
					DisplayNumber(FirstValue, true);
					IsClearEntry = false;
					return;
				}


				if (TextOverwrite && currentOperatorType != OperatorType.NON && !ExpressionEndsWithEqual())
				{
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
				}
				else
				{
					var currentValue = GetCurrentValue();
					PerformPendingCalculation(currentValue);
					if (IsError())
					{
						return;
					}

					DisplayNumber(FirstValue, true);
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
				}

				TextOverwrite = true;
				NumDot = false;
				lastActionWasPercent = false;
				PreserveFormatOnToggle = false;
			}
			catch (OverflowException)
			{
				SetErrorState(Constants.ErrorMessage.OVERFLOW);
			}
		}

		/// <summary>
		/// イコールキーのメイン処理
		/// 保留中の計算を最終確定し、結果を表示 
		/// </summary>
		private void OnEqualsButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			try
			{
				var result = ProcessEqualsLogic();
				if (IsError())
				{
					return;
				}
				DisplayNumber(result, true);

				PreserveFormatOnToggle = false;
				lastActionWasPercent = false;
			}
			catch (InvalidOperationException ex)
			{
				SetErrorState(ex.Message);
			}
			catch (OverflowException)
			{
				SetErrorState(Constants.ErrorMessage.OVERFLOW);
			}
		}

		/// <summary>
		/// '%'キーが押されたときのイベントハンドラ。
		/// 計算機の現在の状態に基づいて、
		/// パーセント計算を行い、画面の表示を更新
		/// </summary>
		private void OnPercentButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (ExpressionEndsWithEqual())
			{
				try
				{
					var r = GetCurrentValue();
					var v = r * CalculatePercent(r);

					FirstValue = v;
					SecondValue = Constants.Numeric.INITIAL_VALUE;
					currentOperatorType = OperatorType.NON;

					DisplayNumber(v, true);
					textExpression.Text = FormatNumberForExpression(v);

					lastActionWasPercent = true;
					PreserveFormatOnToggle = false;
					return;
				}
				catch (OverflowException)
				{
					SetErrorState(Constants.ErrorMessage.OVERFLOW);
					return;
				}
			}

			if (currentOperatorType != OperatorType.NON)
			{
				var rhs = TextOverwrite ? 0m : GetCurrentValue();
				var percentValue = CalculatePercent(rhs); 
				UpdatePercentDisplay(percentValue);           

				lastActionWasPercent = true;
				PreserveFormatOnToggle = false;
				return;
			}

			
			try
			{
				var result = 0m;
				DisplayNumber(result, true);
				textExpression.Text = "0";

				// 計算状態をリセット
				FirstValue = result;
				SecondValue = Constants.Numeric.INITIAL_VALUE;
				currentOperatorType = OperatorType.NON;

				lastActionWasPercent = true;
				PreserveFormatOnToggle = false;
				return;
			}
			catch (OverflowException)
			{
				SetErrorState(Constants.ErrorMessage.OVERFLOW);
				return;
			}
		}


		/// <summary>
		/// クリアエントリーキー入力の処理。式は維持し、表示を 0 に戻す。
		/// </summary>
		private void OnClearEntryButton()
		{
			// エラーの場合はリセット
			if (ShouldResetOnError())
			{
				return;
			}

			var currentExpression = textExpression.Text != null ? textExpression.Text.Trim() : string.Empty;

			if (ExpressionEndsWithEqual())
			{
				if (HasBinaryOperatorInExpression(currentExpression))
				{
					ResetCalculatorState();  
					SetButtonsEnabled(true);  
					return;  
				}

				DisplayZeroResult();  
				ResetCalculationValues(); 
				return;  
			}

			ClearCurrentEntry();
		}

		/// <summary>
		/// 数式列に演算子が含まれているかを判定
		/// ' = '記号より前の部分のみを検査し、加算、減算、乗算、除算のいずれかが含まれていればtrueを返す
		/// </summary>
		/// <param name="expr">検査対象となる数式</param>
		/// <returns>演算子が含まれていればtrue、そうでなければfalse</returns>
		private bool HasBinaryOperatorInExpression(string expr)
		{
			if (string.IsNullOrEmpty(expr))
			{
				return false;
			}

			int eq = expr.LastIndexOf(Constants.Symbol.EQUAL);
			string body = (eq >= 0) ? expr.Substring(0, eq) : expr;

			return body.Contains(Constants.Symbol.ADD) ||
				   body.Contains(Constants.Symbol.SUBTRACT) ||
				   body.Contains(Constants.Symbol.MULTIPLY) ||
				   body.Contains(Constants.Symbol.DIVIDE);
		}


		/// <summary>
		/// 電卓の表示を'0'に設定
		/// </summary>
		private void DisplayZeroResult()
		{
			textResult.Text = Constants.Numeric.ZERO_VALUE;
			TextOverwrite = true;
			NumDot = false;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;
			lastActionWasPercent = false;
		}

		/// <summary>
		/// 計算状態を初期値に戻す
		/// </summary>
		private void ResetCalculationValues()
		{
			FirstValue = Constants.Numeric.INITIAL_VALUE;
			SecondValue = Constants.Numeric.INITIAL_VALUE;
			currentOperatorType = OperatorType.NON;
		}

		/// <summary>
		/// 現在の数値をクリアし、表示を０に戻す
		/// </summary>
		private void ClearCurrentEntry()
		{
			IsClearEntry = true;
			DisplayZeroResult();  
		}

		/// <summary>
		/// クリアキーの処理。全状態を初期化する。
		/// </summary>
		private void OnClearButton()
		{
			ResetAllState();
		}

		/// <summary>
		/// 桁下げキー入力の処理
		/// 末尾1文字削除を行う。
		/// </summary>
		private void OnBackspaceButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (ExpressionEndsWithEqual())
			{
				textExpression.Text = "";
				TextOverwrite = true;
				NumDot = false;
				PreserveFormatOnToggle = false;
				lastActionWasPercent = false;
				ClearedExprAfterEqual = true;
				return;
			}

			if (ClearedExprAfterEqual)
			{
				return;
			}

			if (IsExponentDisplay())
			{
				TextOverwrite = true;
				NumDot = false;
				textResult.Text = Constants.Numeric.ZERO_VALUE;

				PreserveFormatOnToggle = false;
				lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;
				lastActionWasPercent = false;
				return;
			}

			if (TextOverwrite)
			{
				return;
			}

			HandleBackspace();
			UpdateTextResultWithCommas();


			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
			lastActionWasPercent = false;
		}

		/// <summary>
		/// サインチェンジキーの処理。ユーザー入力の見た目保持（末尾ゼロ維持）と、
		/// ＝直後の negate(...) 表記更新に対応する。
		/// </summary>
		private void OnToggleSignButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (string.IsNullOrEmpty(textResult.Text))
			{
				return;
			}

			if (PreserveFormatOnToggle && !IsExponentDisplay())
			{
				string raw = textResult.Text.Replace(",", "");
				raw = ToggleSignRaw(raw);
				SetTextFromRawPreservingCommas(raw);


				TextOverwrite = false;
				NumDot = (raw.IndexOf('.') >= 0);


				UpdateExpressionForToggleSign();


				lastUserTypedRaw = raw;
				lastActionWasPercent = false;
				return;
			}

			ToggleSign();
			UpdateExpressionForToggleSign();

			PreserveFormatOnToggle = false;
			lastActionWasPercent = false;
		}


		/// <summary>
		/// 新しい数値入力を開始する。上書きモードを解除し、小数点フラグを更新。
		/// </summary>
		/// <param name="digit">新しい数値の最初の桁を表す文字列。</param>
		private void StartNewNumber(string digit)
		{
			textResult.Text = digit;
			TextOverwrite = false;
			NumDot = (digit == ".");
		}

		/// <summary>
		/// 既存の入力の末尾に数字を追加する。
		/// </summary>
		/// <param name="digit">追加する数字を表す文字列。</param>
		private void AppendDigit(string digit)
		{
			textResult.Text += digit;
		}

		/// <summary>
		/// ユーザーの入力が有効であるか検証
		/// 最大表示桁数の超過、重複したゼロ、不適切な小数点の入力などをチェック
		/// </summary>
		/// <param name="currentText">現在の表示。</param>
		/// <param name="digit">新たに入力された数字または記号。</param>
		/// <returns>入力が有効であればtrue、そうでなければfalse。</returns>
		private bool IsInputValid(string currentText, string digit)
		{
			bool startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
			int maxDigits = startsWithZeroDot ? Constants.Numeric.MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO : Constants.Numeric.MAX_INTEGER_DISPLAY_DIGITS;

			string nextText = TextOverwrite ? digit : currentText + digit;
			int nextLength = nextText.Replace(".", "").Replace("-", "").Length;

			if (nextLength > maxDigits)
			{
				return false;
			}

			if (!TextOverwrite && currentText == Constants.Numeric.ZERO_VALUE && digit == Constants.Numeric.ZERO_VALUE && !NumDot)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// 指定された演算子に基づき、演算子の記号を返す。
		/// </summary>
		/// <param name="type">演算子</param>
		/// <returns>指定された演算子に対応する記号</returns>
		private string GetOperatorSymbol(OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					{
						return Constants.Symbol.ADD;
					}
				case OperatorType.SUBTRACT:
					{
						return Constants.Symbol.SUBTRACT;
					}
				case OperatorType.MULTIPLY:
					{
						return Constants.Symbol.MULTIPLY;
					}
				case OperatorType.DIVIDE:
					{
						return Constants.Symbol.DIVIDE;
					}
				default:
					{
						return string.Empty;
					}
			}
		}

		/// <summary>
		/// 左辺と右辺の値を指定された演算子タイプに基づいて計算する
		/// </summary>
		/// <param name="left">左辺の値</param>
		/// <param name="right">右辺の値</param>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>計算結果</returns>
		private decimal Calculate(decimal left, decimal right, OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					{
						return left + right;
					}
				case OperatorType.SUBTRACT:
					{
						return left - right;
					}
				case OperatorType.MULTIPLY:
					{
						return left * right;
					}
				case OperatorType.DIVIDE:
					{
						return left / right;
					}
				default:
					{
						return right;
					}
			}
		}

		/// <summary>
		/// 保留中の演算を解決する。未選択なら左辺を現在値に更新し、0除算や0÷0 はエラー化する。
		/// </summary>
		private void PerformPendingCalculation(decimal currentValue)
		{
			if (ExpressionEndsWithEqual() || currentOperatorType == OperatorType.NON)
			{
				FirstValue = currentValue;
			}
			else
			{
				if (currentOperatorType == OperatorType.DIVIDE && currentValue == Constants.Numeric.INITIAL_VALUE)
				{
					if (FirstValue == Constants.Numeric.INITIAL_VALUE)
					{
						SetErrorState(Constants.ErrorMessage.UNDEFINED);
					}
					else
					{
						SetErrorState(Constants.ErrorMessage.DIVIDE_BY_ZERO);
					}
					return;
				}

				decimal result = Calculate(FirstValue, currentValue, currentOperatorType);
				FirstValue = result;
			}
		}

		/// <summary>
		/// 途中式表示を更新する
		/// </summary>
		private void UpdateExpressionDisplay(decimal value, OperatorType type)
		{
			string op = GetOperatorSymbol(type);
			string currentExpr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (!string.IsNullOrEmpty(currentExpr) &&
				!currentExpr.EndsWith(Constants.Symbol.EQUAL) &&
				currentExpr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = currentExpr + " " + op;
				return;
			}

			textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(value), op);
		}

		/// <summary>
		///イコールキーの処理
		/// </summary>
		private decimal ProcessEqualsLogic()
		{
			decimal currentValue = GetCurrentValue();
			bool isFirstEqual = !ExpressionEndsWithEqual();

			if (currentOperatorType == OperatorType.NON)
			{
				SecondValue = currentValue;
				textExpression.Text = string.Format("{0} {1}",
					FormatNumberForExpression(currentValue), Constants.Symbol.EQUAL);
				FirstValue = currentValue;
				return currentValue;
			}

			decimal left, right;

			if (isFirstEqual)
			{
				left = FirstValue;
				right = currentValue;
				SecondValue = currentValue;
			}
			else
			{
				left = FirstValue;
				right = SecondValue;
			}

			if (currentOperatorType == OperatorType.DIVIDE && right == Constants.Numeric.INITIAL_VALUE)
			{
				if (left == Constants.Numeric.INITIAL_VALUE)
				{
					throw new InvalidOperationException(Constants.ErrorMessage.UNDEFINED);
				}
				else
				{
					throw new InvalidOperationException(Constants.ErrorMessage.DIVIDE_BY_ZERO);
				}
			}

			decimal result = Calculate(left, right, currentOperatorType);
			FirstValue = result;

			string opSym = GetOperatorSymbol(currentOperatorType);
			string leftExpr = FormatNumberForExpression(left);
			string rightExpr = FormatNumberForExpression(right);

			string curr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (!string.IsNullOrEmpty(curr) &&
				!curr.EndsWith(Constants.Symbol.EQUAL) &&
				curr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				if (curr.EndsWith(opSym))
				{
					textExpression.Text = curr + " " + rightExpr + " " + Constants.Symbol.EQUAL;
				}
				else
				{
					textExpression.Text = curr + " " + Constants.Symbol.EQUAL;
				}
			}
			else
			{
				textExpression.Text = string.Format("{0} {1} {2} {3}",
					leftExpr, opSym, rightExpr, Constants.Symbol.EQUAL);
			}

			return result;
		}


		/// <summary>
		/// 現在値を％形式に変換する。
		/// </summary>
		private decimal CalculatePercent(decimal value)
		{
			return value * Constants.Numeric.PERCENT_MULTIPLY;
		}

		/// <summary>
		/// ％の表示と式を更新する。加減算は A*(B/100)、乗除算は B/100 として扱う。
		/// 表示は編集継続可能な状態にする。
		/// </summary>
		private void UpdatePercentDisplay(decimal percentValue)
		{
			decimal previousValue = FirstValue;
			decimal calculatedValue;

			if (currentOperatorType == OperatorType.ADD || currentOperatorType == OperatorType.SUBTRACT)
			{
				calculatedValue = previousValue * percentValue;
				textExpression.Text = string.Format("{0} {1} {2}",
					FormatNumberForExpression(previousValue),
					GetOperatorSymbol(currentOperatorType),
					FormatNumberForExpression(calculatedValue));
			}
			else
			{
				calculatedValue = percentValue; // B% = B/100
				textExpression.Text = string.Format("{0} {1} {2}",
					FormatNumberForExpression(previousValue),
					GetOperatorSymbol(currentOperatorType),
					FormatNumberForExpression(calculatedValue));
			}


			DisplayNumber(calculatedValue, false);
		}

		/// <summary>
		/// 結果表示の末尾 1 文字を削除する。空や単独の「-」になった場合は 0 に戻す。
		/// </summary>
		private void HandleBackspace()
		{
			string currentText = textResult.Text.Replace(",", "");
			if (currentText.Length > 0)
			{
				string newText = currentText.Substring(0, currentText.Length - 1);

				if (string.IsNullOrEmpty(newText) || newText == "-")
				{
					textResult.Text = Constants.Numeric.ZERO_VALUE;
					TextOverwrite = true;
					NumDot = false;
				}
				else
				{
					textResult.Text = newText;
					NumDot = textResult.Text.Contains(".");
				}
			}
			else
			{
				ResetCalculatorState();
			}
		}

		/// <summary>
		/// 数値として符号反転を行い表示を更新
		/// </summary>
		private void ToggleSign()
		{
			DisplayValue = GetCurrentValue();
			DisplayValue = -DisplayValue;
			DisplayNumber(DisplayValue, false);
			isNegated = true;
		}

		/// <summary>
		/// カンマ無しの文字列の符号のみを反転する
		/// </summary>
		private string ToggleSignRaw(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return raw;
			}
			if (raw[0] == '-')
			{
				return raw.Substring(1);
			}
			return "-" + raw;
		}

		/// <summary>
		/// 文字列をそのまま結果欄に反映し、カンマ付与を行う。
		/// </summary>
		private void SetTextFromRawPreservingCommas(string raw)
		{
			textResult.Text = raw;
			UpdateTextResultWithCommas();
		}

		/// <summary>
		/// イコールキー入力直後の サインチェンジキーを入力したとき negate(...) の入れ子表記で更新する。
		/// 例）"100 =" → "negate(100) " → さらに ± → "negate(negate(100))"
		/// </summary>
		private void UpdateExpressionForToggleSign()
		{
			string expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (expr.EndsWith(Constants.Symbol.EQUAL))
			{
				int eq = expr.LastIndexOf(Constants.Symbol.EQUAL);
				string body = (eq >= 0 ? expr.Substring(0, eq) : expr).Trim();
				if (string.IsNullOrEmpty(body)) body = FormatNumberForExpression(FirstValue);

				textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + body + ")";
				return;
			}

			if (expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + expr + ")";
			}
		}


		/// <summary>
		/// 表示の丸め、 カンマ付与 、 状態更新を行う。
		/// </summary>
		private void DisplayNumber(decimal value, bool overwrite)
		{
			decimal rounded = RoundResult(value);
			textResult.Text = FormatNumberForDisplay(rounded);
			UpdateTextResultWithCommas();
			TextOverwrite = overwrite;
			NumDot = false;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 結果表示の文字列に 3 桁区切りカンマを付与する
		/// </summary>
		private void UpdateTextResultWithCommas()
		{
			if (IsError())
			{
				return;
			}
			if (IsExponentDisplay())
			{
				return;
			}

			string currentText = textResult.Text.Replace(",", "");
			if (string.IsNullOrEmpty(currentText) || currentText == "-" || (currentText == "0" && !NumDot))
			{
				return;
			}

			bool isNegative = currentText.StartsWith("-");
			if (isNegative)
			{
				currentText = currentText.Substring(1);
			}

			int dotIndex = currentText.IndexOf('.');
			string integerPart = currentText;
			string decimalPart = "";

			if (dotIndex != -1)
			{
				integerPart = currentText.Substring(0, dotIndex);
				decimalPart = currentText.Substring(dotIndex + 1);
			}

			try
			{
				decimal integerValue;
				if (decimal.TryParse(integerPart, NumberStyles.Number, CultureInfo.InvariantCulture, out integerValue))
				{
					string formattedInteger = integerValue.ToString("#,##0", CultureInfo.InvariantCulture);
					string newText = formattedInteger;

					if (dotIndex != -1)
					{
						newText += "." + decimalPart;
					}
					if (isNegative)
					{
						newText = "-" + newText;
					}

					textResult.Text = newText;
				}
			}
			catch (FormatException)
			{
				// 何もしない
			}
		}

		/// <summary>
		/// 途中式用の数値整形を行う。
		/// </summary>
		private string FormatNumberForExpression(decimal value)
		{
			return FormatNumberForDisplay(value);
		}

		/// <summary>
		/// 指数表記の整形を行う。指数は小文字 e、不要な 0/小数点を整理する。
		/// </summary>
		private string FormatExponential(decimal value)
		{
			var expString = value.ToString("e", CultureInfo.InvariantCulture); 
			var parts = expString.Split('e');
			var mantissa = parts[0].TrimEnd('0');
			if (mantissa.EndsWith("."))
			{
				mantissa = mantissa.TrimEnd('.');
			}
			if (!mantissa.Contains("."))
			{
				mantissa += ".";
			}

			string exponent = Regex.Replace(parts[1], @"^(\+|-)(0)(\d+)", "$1$3");
			return mantissa + "e" + exponent;
		}

		

		/// <summary>
		/// 数値の大きさが表示可能な桁数を超える場合、指数表記に変換する
		/// </summary>
		private string FormatNumberForDisplay(decimal value)
		{
			decimal abs = Math.Abs(value);
			if (abs == 0m)
			{
				return Constants.Numeric.ZERO_VALUE;
			}

			string fixedStr = value.ToString("0.#############################", CultureInfo.InvariantCulture);

			if (abs >= 1m)
			{
				int dot = fixedStr.IndexOf('.');
				bool neg = (fixedStr[0] == '-');
				int intLen = (dot >= 0 ? dot : fixedStr.Length) - (neg ? 1 : 0);

				if (intLen > Constants.Numeric.MAX_INTEGER_DISPLAY_DIGITS)
				{
					return FormatExponential(value);
				}

				return fixedStr;
			}
			else
			{
				int dot = fixedStr.IndexOf('.');
				int leadingZeros = 0;

				for (int i = dot + 1; i < fixedStr.Length && fixedStr[i] == '0'; i++)
				{
					leadingZeros++;
				}

				int totalFractionDigits = fixedStr.Length - dot - 1;
				int significantDigits = fixedStr.TrimStart('0').Replace(".", "").Length;

				if (leadingZeros >=Constants.Numeric.MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO ||
					totalFractionDigits > Constants.Numeric.MAX_TOTAL_FRACTION_DIGITS ||
					significantDigits > Constants.Numeric.MAX_SIGNIFICANT_DIGITS)
				{
					return FormatExponential(value);
				}

				return fixedStr;
			}
		}



		/// <summary>
		/// 現在の表示文字列を decimal に変換する
		/// </summary>
		private decimal GetCurrentValue()
		{
			return ParseDisplayToDecimal(textResult.Text);
		}

		/// <summary>
		/// 表示文字列を decimal に変換する
		/// </summary>
		private decimal ParseDisplayToDecimal(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return Constants.Numeric.INITIAL_VALUE;
			}

			string s = text.Replace(",", "");
			decimal dv;

			if (s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
			{
				double dd;
				if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out dd))
				{
					try
					{
						return (decimal)dd;
					}
					catch
					{
						return Constants.Numeric.INITIAL_VALUE;
					}
				}
				return Constants.Numeric.INITIAL_VALUE;
			}

			if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				return dv;
			}
			return Constants.Numeric.INITIAL_VALUE;
		}

		/// <summary>
		/// 数値の絶対値に応じて計算結果を丸める
		/// </summary>
		private decimal RoundResult(decimal value)
		{
			decimal abs = Math.Abs(value);


			if (abs > 0m && abs < 1m)
			{
				return Math.Round(value, 16, MidpointRounding.AwayFromZero);
			}

			if (abs >= 1m)
			{
				string integerPartStr = Math.Floor(abs).ToString(CultureInfo.InvariantCulture);
				int integerLength = integerPartStr.Length;

				int decimalPlacesToRound = 16 - integerLength;
				if (decimalPlacesToRound >= 0)
				{
					return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
				}
			}

			return value;
		}


		/// <summary>
		/// エラー状態かどうかを返す。
		/// </summary>
		private bool IsError()
		{
			return IsErrorState;
		}

		/// <summary>
		/// 式表示が ＝ で終わっているか判定する
		/// </summary>
		private bool ExpressionEndsWithEqual()
		{
			return textExpression.Text.Length > 0 && textExpression.Text.EndsWith(Constants.Symbol.EQUAL);
		}

		/// <summary>
		/// 現在の結果表示が指数表記か判定する。
		/// </summary>
		private bool IsExponentDisplay()
		{
			string t = textResult.Text;
			return (t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0);
		}

		/// <summary>
		/// 
		/// </summary>
		private bool ShouldResetOnError()
		{
			if (IsErrorState)
			{
				ResetCalculatorState();
				return true;
			}
			return false;
		}

		/// <summary>
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		private void AutoFitResultFont()
		{
			float size = defaultFontSize; 

			FontFamily family = textResult.Font.FontFamily;
			FontStyle style = textResult.Font.Style;

			while (size > Constants.FontSize.MIN_LIMIT)
			{
				using (Font trial = new Font(family, size, style))
				{
					Size proposed = new Size(int.MaxValue, int.MaxValue);
					TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
					Size sz = TextRenderer.MeasureText(textResult.Text, trial, proposed, flags);

					if (sz.Width <= textResult.ClientSize.Width)
					{
						if (Math.Abs(textResult.Font.Size - size) > 0.1f)
						{
							Font old = textResult.Font;
							textResult.Font = new Font(family, size, style);
							old.Dispose();
						}
						return;
					}
				}
				size -= Constants.FontSize.SIZE_EPSILON;
			}

			if (Math.Abs(textResult.Font.Size - Constants.FontSize.MIN_LIMIT) > Constants.FontSize.REDUCTION_STEP)
			{
				Font oldFinal = textResult.Font;
				textResult.Font = new Font(family, Constants.FontSize.MIN_LIMIT, style);
				oldFinal.Dispose();
			}
		}

		/// <summary>
		/// エラー時に一部キーの活性/非活性をまとめて切り替える。
		/// </summary>
		private void SetButtonsEnabled(bool enabled)
		{
			foreach (Button btn in DisabledButtonsOnError)
			{
				btn.Enabled = enabled;
			}
		}

		/// <summary>
		/// エラー状態を設定し、メッセージ表示・フォントサイズ調整・ボタン無効化を行う。
		/// </summary>
		private void SetErrorState(string message)
		{
			textResult.Text = message;

			float sz = Constants.FontSize.ERROR_MESSAGE;
			if (sz < Constants.FontSize.MIN_LIMIT)
			{
				sz = Constants.FontSize.MIN_LIMIT;
			}
			textResult.Font = new Font(textResult.Font.FontFamily, sz, textResult.Font.Style);

			IsErrorState = true;
			SetButtonsEnabled(false);
		}

		/// <summary>
		/// 入力開始前の状態を整える。エラー中や ＝直後の場合は状態を初期化する。
		/// </summary>
		private void HandleInitialState()
		{
			if (IsErrorState || ExpressionEndsWithEqual())
			{
				ResetCalculatorState();
				ClearedExprAfterEqual = false;
			}
		}

		/// <summary>
		/// 全体のリセット処理である。計算状態と UI 状態を初期化し、ボタンを活性化する。
		/// </summary>
		private void ResetAllState()
		{
			ResetCalculatorState();
			SetButtonsEnabled(true);
		}

		/// <summary>
		/// 計算機内部状態と表示、フォントの初期化を行う
		/// </summary>
		private void ResetCalculatorState()
		{
			FirstValue = Constants.Numeric.INITIAL_VALUE;
			SecondValue = Constants.Numeric.INITIAL_VALUE;
			currentOperatorType = OperatorType.NON;
			textExpression.Text = "";
			textResult.Text = Constants.Numeric.ZERO_VALUE;
			TextOverwrite = true;
			NumDot = false;
			IsErrorState = false;
			IsClearEntry = false;
			DisplayValue = Constants.Numeric.INITIAL_VALUE;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;
			lastActionWasPercent = false;
			ClearedExprAfterEqual = false;


			textResult.Font = new Font(textResult.Font.FontFamily, defaultFontSize, textResult.Font.Style);


			textExpression.Font = new Font(textExpression.Font.FontFamily, defaultExpressionFontSize, textExpression.Font.Style);

			AutoFitResultFont();
		}
	}
}
